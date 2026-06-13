namespace Grimoire.Infrastructure.Persistence.Database;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class AuditInterceptor : SaveChangesInterceptor {
	public override InterceptionResult<int> SavingChanges(
		DbContextEventData eventData,
		InterceptionResult<int> result) {
		
		UpdateAuditFields(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default) {
		
		UpdateAuditFields(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private static void UpdateAuditFields(DbContext? context) {
		if (context is null) return;

		foreach (var entry in context.ChangeTracker.Entries<BaseModel>().Where(e => e.State == EntityState.Modified)) {
			entry.Entity.MarkAsUpdated();
		}
	}
}
