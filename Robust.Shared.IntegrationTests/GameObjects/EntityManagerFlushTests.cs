#if EXCEPTION_TOLERANCE
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestFixture, Parallelizable]
internal sealed class EntityManagerFlushTests
{
    private static EntityManager Setup()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        sim.Resolve<IConfigurationManager>().SetCVar("robust.tests.failure_log_level", LogLevel.Fatal);
        return (EntityManager) sim.Resolve<IEntityManager>();
    }

    [Test]
    public void Flush_EntityStuckTerminating_IsDeleted()
    {
        var entMan = Setup();
        var uid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        entMan.SetLifeStage(entMan.GetComponent<MetaDataComponent>(uid), EntityLifeStage.Terminating);

        Assert.DoesNotThrow(entMan.FlushEntities);
        Assert.That(entMan.EntityCount, Is.Zero);
        Assert.That(entMan.Deleted(uid));
    }

    [Test]
    public void Flush_EntityStuckDeleted_IsPurged()
    {
        var entMan = Setup();
        var uid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        entMan.SetLifeStage(entMan.GetComponent<MetaDataComponent>(uid), EntityLifeStage.Deleted);

        Assert.DoesNotThrow(entMan.FlushEntities);
        Assert.That(entMan.EntityCount, Is.Zero);
        Assert.That(entMan.HasComponent<TransformComponent>(uid), Is.False);
    }
}
#endif
