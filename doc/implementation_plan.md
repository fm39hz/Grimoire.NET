# Implementation Plan - Server-Sent Events (SSE) for Real-Time Job Progress

We will implement a real-time progress update mechanism using Server-Sent Events (SSE). This will replace potential polling patterns, decrease database connection load (write throttling), and provide immediate feedback in the Obsidian plugin UI when importing books.

---

## Proposed Changes

### Backend (Grimoire.NET)

To stream background job progress using SSE efficiently without overloading the database, we will use a hybrid model:
1. **In-Memory Pub/Sub**: Real-time progress updates are published in-process using `System.Threading.Channels` and routed directly to active client streams.
2. **State Transition Filter**: A Hangfire state filter will capture the job's terminal status (`Succeeded`/`Failed`) and publish them to the channel.
3. **Database Write Throttling**: The job executor will only write to the Hangfire `jobparameter` table when progress changes by >=1% or after 500ms, preventing database write storms while notifying the SSE channel instantly.

#### [NEW] [JobProgressTracker.cs](file:///home/fm39hz/Workspace/Personal/Books/Grimoire.NET/src/Grimoire.Api/Publish/JobProgressTracker.cs)
Create a new singleton progress tracker using `System.Threading.Channels`:
- Interface `IJobProgressTracker` and implementation `JobProgressTracker`.
- Maintain a thread-safe registry of `ChannelWriter<PublishJobStatusDto>` grouped by `jobId`.
- Methods:
  - `UpdateProgress(string jobId, int progress)`: Broadcasts real-time progress percentage.
  - `CompleteJob(string jobId, string? downloadUrl)`: Broadcasts completion event and closes channels.
  - `FailJob(string jobId, string error)`: Broadcasts failure details and closes channels.
  - `Subscribe(string jobId, CancellationToken cancellationToken)`: Returns an `IAsyncEnumerable<PublishJobStatusDto>`.

#### [NEW] [HangfireJobStateFilter.cs](file:///home/fm39hz/Workspace/Personal/Books/Grimoire.NET/src/Grimoire.Api/Publish/HangfireJobStateFilter.cs)
Create a Hangfire global state filter `HangfireJobStateFilter` that implements `IStateFilter`:
- Hooks into `OnStateApplied` when a job transitions state.
- When a job moves to `SucceededState`, reads the `JobResult` outcome and invokes `CompleteJob(jobId, result.DownloadUrl)`.
- When a job moves to `FailedState`, gets the exception/error details and invokes `FailJob(jobId, errorMessage)`.

#### [MODIFY] [Program.cs](file:///home/fm39hz/Workspace/Personal/Books/Grimoire.Api/Program.cs)
- Register `JobProgressTracker` as a Singleton service:
  ```csharp
  builder.Services.AddSingleton<IJobProgressTracker, JobProgressTracker>();
  ```
- Register the global state filter at startup:
  ```csharp
  var progressTracker = app.Services.GetRequiredService<IJobProgressTracker>();
  GlobalJobFilters.Filters.Add(new HangfireJobStateFilter(progressTracker));
  ```

#### [MODIFY] [ImportJob.cs](file:///home/fm39hz/Workspace/Personal/Books/Grimoire.NET/src/Grimoire.Job/Jobs/ImportJob.cs)
- Inject `IJobProgressTracker`.
- Optimize the `OnProgress` action to:
  1. Notify the in-memory tracker instantly (fast, zero DB transactions).
  2. Write to the Hangfire `JobStorage` parameter *only* when the progress changes by >= 1% or when 500ms have elapsed since the last DB write.

#### [MODIFY] [PublishController.cs](file:///home/fm39hz/Workspace/Personal/Books/Grimoire.NET/src/Grimoire.Api/Controller/PublishController.cs)
- Add SSE endpoint:
  ```csharp
  [HttpGet("jobs/{jobId}/progress")]
  public async Task GetProgressStream(string jobId, CancellationToken cancellationToken)
  ```
- Send initial status from `publishService.GetJobStatusAsync`.
- Stream upcoming updates via `IJobProgressTracker.Subscribe` using the MIME type `text/event-stream`.
- Spin a lightweight background safety poller (e.g. checks job status every 3 seconds) as a fallback mechanism to handle server restarts or race conditions gracefully.

---

### Frontend (grimoire-obsidian)

In the Obsidian plugin, we will consume the SSE stream using the browser-native `EventSource` API and update the status bar UI.

#### [MODIFY] [jobs.ts](file:///home/fm39hz/Workspace/Personal/Books/grimoire-obsidian/src/api/jobs.ts)
- Add helper method `getProgressStream(jobId: string): EventSource`:
  ```typescript
  getProgressStream(jobId: string): EventSource {
      const baseUrl = (this.client as any).baseUrl;
      return new EventSource(`${baseUrl}/api/v1/publishes/jobs/${jobId}/progress`);
  }
  ```

#### [MODIFY] [main.ts](file:///home/fm39hz/Workspace/Personal/Books/grimoire-obsidian/src/main.ts)
- Update `pushStagedEpubs()`:
  - After successfully enqueuing the import job, obtain the `EventSource` using `this.api!.jobs.getProgressStream(job.jobId)`.
  - Listen to `onmessage` and update `this.statusBar` status to `pushing` with the progress percentage and active file name:
    ```typescript
    this.statusBar?.update({
        status: "pushing",
        progress: {
            current: jobStatus.progress ?? 0,
            total: 100,
            message: `Importing ${file.name}: ${jobStatus.progress ?? 0}%`
        }
    });
    ```
  - Close the event source and revert status bar status to `idle` upon receiving `"Completed"` or `"Failed"` statuses.
  - Automatically call `this.refreshBookTreeView()` on completion.

---

## Verification Plan

### Automated Tests
- Run backend integration test suite to verify no regressions:
  ```bash
  make verify
  ```
- Implement a blackbox test or modify `test/tests/epub.blackbox.test.ts` to consume the SSE stream endpoint `/publishes/jobs/{jobId}/progress` using `fetch` with raw stream reading to assert that progress events are sent properly in SSE formatting.

### Manual Verification
- Deploy the updated Obsidian plugin locally.
- Place a large EPUB file in the `Stagings` folder.
- Execute the `push-staged-epubs` command from the Obsidian Command Palette.
- Verify that the Obsidian status bar displays real-time progress percentages (`Importing [File]: X%`) dynamically as the import runs.
