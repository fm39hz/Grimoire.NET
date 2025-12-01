## 1\. Use Case Diagram (Cập nhật)

```mermaid
graph LR
    %% Actors
    User("👤 The Keeper<br/>(User)"****)
    Scraper("🤖 Scraper Tool<br/>(Client Script)")

    %% Packages as Subgraphs
    subgraph Collector ["The Collector (Nhập liệu)"]
        direction TB
        UC1(["Upload Assets<br/>(Images/Covers)"])
        UC2(["Import Chapter Data<br/>(JSON with AssetKeys)"])
    end

    subgraph Librarian ["The Librarian (Quản lý Metadata)"]
        direction TB
        UC3(["Update Series Metadata<br/>(Title, Author, Tags)"])
        UC4(["Update Volume Metadata<br/>(Cover, Illustrator)"])
    end

    subgraph Bindery ["The Bindery (Xuất bản)"]
        direction TB
        UC5(["Export Series Anthology"])
        UC6(["Export Single Volume"])
    end

    %% Relationships
    Scraper --> UC1
    Scraper --> UC2
    
    User --> UC3
    User --> UC4
    User --> UC5
    User --> UC6

    %% Styling (Optional - giúp nó giống Use Case hơn)
    classDef usecase fill:#fff,stroke:#333,stroke-width:1px,rx:5px,ry:5px;
    class UC1,UC2,UC3,UC4,UC5,UC6 usecase;
```

-----

## 2\. Sequence Diagrams (Chi tiết Luồng xử lý Mới)

### 2.1. Use Case: The "Smart Import" Workflow (Client-Side Scraping)

```mermaid
sequenceDiagram
    autonumber
    participant Tool as Client Tool (Python)
    participant API as Collector Slice
    participant MinIO
    participant DB as PostgreSQL

    note over Tool, DB: Giai đoạn 1: Upload Assets (Images)
    
    loop For Each Image in Chapter
        Tool->>Tool: Download Image from Web
        Tool->>API: POST /assets/upload (File Stream)
        activate API
        API->>MinIO: PutObjectAsync()
        MinIO-->>API: Success
        API-->>Tool: Return AssetKey<br/>(e.g., "vol1/chap1/img01.jpg")
        deactivate API
    end

    note over Tool, DB: Giai đoạn 2: Submit Content
    
    Tool->>Tool: Replace Image URLs with AssetKeys in JSON
    Tool->>API: POST /import/chapter (Clean JSON)
    activate API
    API->>API: Validate JSON
    API->>DB: Save Chapter (JSONB)
    DB-->>API: Success
    API-->>Tool: 201 Created
    deactivate API
```

### 2.2. Use Case: Manage Metadata (The Librarian)

```mermaid
sequenceDiagram
    autonumber
    participant User
    participant API as Librarian Slice
    participant DB as PostgreSQL

    %% Scenario: Update Series
    note over User, DB: Cập nhật thông tin Series
    User->>API: PUT /series/{id}/metadata<br/>{Author: "...", Tags: ["Dark", "Fantasy"]}
    activate API
    API->>DB: Load Series
    API->>API: Update Metadata JSONB Column
    API->>DB: SaveChangesAsync()
    API-->>User: 200 OK
    deactivate API

    %% Scenario: Update Volume
    note over User, DB: Cập nhật thông tin Volume
    User->>API: PUT /volumes/{id}/metadata<br/>{Illustrator: "...", Description: "..."}
    activate API
    API->>DB: Load Volume
    API->>API: Update Metadata JSONB Column
    API->>DB: SaveChangesAsync()
    API-->>User: 200 OK
    deactivate API
```

### 2.3. Use Case: Export Anthology (The Bindery)

```mermaid
sequenceDiagram
    autonumber
    participant User
    participant API
    participant Hangfire
    participant DB
    participant MinIO

    User->>API: POST /bindery/series/{id}/bind (Mode=Anthology)
    API->>Hangfire: Enqueue Job
    
    activate Hangfire
    Hangfire->>DB: Get Series Metadata (Title, Author, CoverKey)
    Hangfire->>DB: Get All Volumes + Chapters
    
    note right of Hangfire: Bắt đầu đóng sách
    
    Hangfire->>MinIO: Download Cover Image (using CoverKey)
    Hangfire->>Hangfire: Add Cover Page
    
    loop Each Volume
        Hangfire->>Hangfire: Add "Volume Separator" Page<br/>(Title + Summary from Metadata)
        loop Each Chapter
             Hangfire->>Hangfire: Render Content
        end
    end
    
    Hangfire->>MinIO: Upload Final EPUB
    deactivate Hangfire
```

-----

## 3\. Kiến trúc Hệ thống (Cập nhật trách nhiệm)

```mermaid
graph TD
  %% LAYER 1 — CLIENT
  subgraph L1["Client Layer"]
    direction TB
    Scraper["Python Scraper Tool"]
    Browser["Web Dashboard"]
  end

  %% LAYER 2 — GRIMOIRE CORE (.NET 9)
  subgraph L2["GRIMOIRE Core (.NET 9)"]
    direction TB
    API["API Controllers<br/>(Entry Point)"]

    subgraph CoreLogic["Core Logic (Behind API)"]
      direction TB
      Col["Collector<br/>(Receive File & JSON)"]
      Lib["Librarian<br/>(Update Metadata JSONB)"]
      Bind["Bindery<br/>(Render EPUB)"]
      Job["Hangfire Worker"]
    end
  end

  %% LAYER 3 — INFRASTRUCTURE
  subgraph L3["Infrastructure"]
    direction TB
    DB[(PostgreSQL)]
    Sto[(MinIO)]
  end

  %% FLOWS
  Scraper -->|Upload File| API
  Scraper -->|Send JSON| API
  Browser -->|UI Actions| API

  API --> Col
  API --> Lib
  API --> Bind

  Col -->|Store File| Sto
  Col -->|Save Metadata| DB

  Lib -->|Update JSONB| DB

  Bind -->|Queue Job| Job

  Job -->|Read Metadata| DB
  Job -->|Read Assets| Sto
  Job -->|Write EPUB| Sto

```
