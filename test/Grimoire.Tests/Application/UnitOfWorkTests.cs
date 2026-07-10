namespace Grimoire.Tests.Application;

using System.Threading.Tasks;
using Grimoire.Tests.TestInfrastructure;
using Xunit;

public class UnitOfWorkTests {
	[Fact]
	public async Task NoOpUnitOfWork_Should_Execute_PostCommitAction_Immediately() {
		var uow = new NoOpUnitOfWork();
		var executed = false;

		uow.RegisterPostCommitAction(() => {
			executed = true;
			return Task.CompletedTask;
		});

		// Wait briefly since it runs in Task.Run
		await Task.Delay(50);

		Assert.True(executed);
	}
}
