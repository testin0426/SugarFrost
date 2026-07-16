using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ISpawnManager"/>
    /// <remarks>
    /// Initialization order maxed, as spawned prefabs (eg, loaded when restoring saved game)
    /// may use other engine services.
    /// </remarks>
    [InitializeAtRuntime(int.MaxValue)]
    public class SpawnManager : IStatefulService<GameStateMap>, ISpawnManager
    {
        [Serializable]
        public class GameState
        {
            public SpawnedObjectState[] SpawnedObjects;
        }

        public virtual SpawnConfiguration Configuration { get; }

        protected virtual Dictionary<string, SpawnedObject> SpawnedByPath { get; } = new();
        protected virtual IResourceProviderManager Providers { get; }
        protected virtual ResourceLoader<GameObject> Loader { get; private set; }
        protected virtual GameObject Container { get; private set; }

        public SpawnManager (SpawnConfiguration cfg, IResourceProviderManager providers)
        {
            Configuration = cfg;
            Providers = providers;
        }

        public virtual Awaitable InitializeService ()
        {
            Loader = Configuration.Loader.CreateFor<GameObject>(Providers);
            Container = Engine.CreateObject(new() { Name = "Spawn" });
            return Async.Completed;
        }

        public virtual void ResetService ()
        {
            DestroyAllSpawned();
        }

        public virtual void DestroyService ()
        {
            DestroyAllSpawned();
            Loader?.ReleaseAll(this);
            ObjectUtils.DestroyOrImmediate(Container);
        }

        public virtual void SaveServiceState (GameStateMap map)
        {
            var state = new GameState {
                SpawnedObjects = SpawnedByPath.Values
                    .Select(o => new SpawnedObjectState(o)).ToArray()
            };
            map.SetState(state);
        }

        public virtual async Awaitable LoadServiceState (GameStateMap map)
        {
            var state = map.GetState<GameState>();
            if (state?.SpawnedObjects is { Length: > 0 }) await LoadObjects();
            else if (SpawnedByPath.Count > 0) DestroyAllSpawned();

            async Awaitable LoadObjects ()
            {
                using var _ = Async.Rent(out var tasks);
                var toDestroy = SpawnedByPath.Values.ToList();
                foreach (var objState in state.SpawnedObjects)
                    if (IsSpawned(objState.Path)) UpdateObject(objState);
                    else tasks.Add(SpawnObject(objState));
                foreach (var obj in toDestroy)
                    DestroySpawned(obj.Path);
                await Async.All(tasks);

                async Awaitable SpawnObject (SpawnedObjectState objState)
                {
                    var spawned = await Spawn(objState.Path);
                    objState.ApplyTo(spawned);
                    spawned.AwaitSpawn(AsyncToken.CompletedToken).Forget();
                }

                void UpdateObject (SpawnedObjectState objState)
                {
                    var spawned = this.GetSpawnedOrErr(objState.Path);
                    toDestroy.Remove(spawned);
                    objState.ApplyTo(spawned);
                    spawned.AwaitSpawn(AsyncToken.CompletedToken).Forget();
                }
            }
        }

        public virtual async Awaitable HoldResources (string path, object holder)
        {
            var resPath = SpawnConfiguration.ProcessInputPath(path, out _);
            await Loader.Load(resPath, holder);
        }

        public virtual void ReleaseResources (string path, object holder)
        {
            var resPath = SpawnConfiguration.ProcessInputPath(path, out _);
            if (!Loader.IsLoaded(resPath)) return;

            Loader.Release(resPath, holder, false);
            if (Loader.CountHolders(resPath) == 0)
            {
                if (IsSpawned(path)) DestroySpawned(path);
                Loader.Release(resPath, holder);
            }
        }

        public virtual async Awaitable<SpawnedObject> Spawn (string path,
            InstantiateOptions options = default, AsyncToken token = default)
        {
            if (IsSpawned(path)) throw new Error($"Object '{path}' is already spawned.");
            var resPath = SpawnConfiguration.ProcessInputPath(path, out _);
            var res = await Loader.LoadOrErr(resPath, this);
            token.ThrowIfCanceled();
            options.Name = string.IsNullOrWhiteSpace(options.Name) ? path : options.Name;
            options.Parent = options.Parent ? options.Parent : Container.transform;
            var obj = await Engine.Instantiate(res.Object, options, token);
            return SpawnedByPath[path] = new(path, obj);
        }

        public virtual void CollectSpawned (ICollection<SpawnedObject> spawned)
        {
            foreach (var obj in SpawnedByPath.Values)
                spawned.Add(obj);
        }

        public virtual bool IsSpawned (string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return SpawnedByPath.ContainsKey(path);
        }

        public virtual SpawnedObject GetSpawned (string path)
        {
            return SpawnedByPath.GetValueOrDefault(path);
        }

        public virtual void DestroySpawned (string path, bool dispose = true)
        {
            if (SpawnedByPath.Remove(path, out var spawned) && dispose)
                ObjectUtils.DestroyOrImmediate(spawned.GameObject);
        }

        protected virtual void DestroyAllSpawned ()
        {
            foreach (var spawned in SpawnedByPath.Values)
                ObjectUtils.DestroyOrImmediate(spawned.GameObject);
            SpawnedByPath.Clear();
        }
    }
}
