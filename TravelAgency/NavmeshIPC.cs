﻿using Dalamud.Plugin.Ipc;
using ECommons;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace TravelAgency;
public class NavmeshIPC
{
    internal static readonly string Name = "vnavmesh";
    private static ICallGateSubscriber<bool>? _navIsReady;
    private static ICallGateSubscriber<float>? _navBuildProgress;
    private static ICallGateSubscriber<bool>? _navReload;
    private static ICallGateSubscriber<bool>? _navRebuild;
    private static ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>? _navPathfind;
    private static ICallGateSubscriber<bool>? _navIsAutoLoad;
    private static ICallGateSubscriber<bool, object>? _navSetAutoLoad;

    private static ICallGateSubscriber<Vector3, float, float, Vector3?>? _queryMeshNearestPoint;
    private static ICallGateSubscriber<Vector3, bool, float, Vector3?>? _queryMeshPointOnFloor;

    private static ICallGateSubscriber<List<Vector3>, bool, object>? _pathMoveTo;
    private static ICallGateSubscriber<object>? _pathStop;
    private static ICallGateSubscriber<bool>? _pathIsRunning;
    private static ICallGateSubscriber<int>? _pathNumWaypoints;
    private static ICallGateSubscriber<bool>? _pathGetMovementAllowed;
    private static ICallGateSubscriber<bool, object>? _pathSetMovementAllowed;
    private static ICallGateSubscriber<bool>? _pathGetAlignCamera;
    private static ICallGateSubscriber<bool, object>? _pathSetAlignCamera;
    private static ICallGateSubscriber<float>? _pathGetTolerance;
    private static ICallGateSubscriber<float, object>? _pathSetTolerance;

    private static ICallGateSubscriber<Vector3, bool, bool>? _pathfindAndMoveTo;
    private static ICallGateSubscriber<bool>? _pathfindInProgress;

    internal static void Init()
    {
        try
        {
            _navIsReady = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Nav.IsReady");
            _navBuildProgress = Svc.PluginInterface.GetIpcSubscriber<float>($"{Name}.Nav.BuildProgress");
            _navReload = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Nav.Reload");
            _navRebuild = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Nav.Rebuild");
            _navPathfind = Svc.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>($"{Name}.Nav.Pathfind");
            _navIsAutoLoad = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Nav.IsAutoLoad");
            _navSetAutoLoad = Svc.PluginInterface.GetIpcSubscriber<bool, object>($"{Name}.Nav.SetAutoLoad");

            _queryMeshNearestPoint = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>($"{Name}.Query.Mesh.NearestPoint");
            _queryMeshPointOnFloor = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, Vector3?>($"{Name}.Query.Mesh.PointOnFloor");

            _pathMoveTo = Svc.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>($"{Name}.Path.MoveTo");
            _pathStop = Svc.PluginInterface.GetIpcSubscriber<object>($"{Name}.Path.Stop");
            _pathIsRunning = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Path.IsRunning");
            _pathNumWaypoints = Svc.PluginInterface.GetIpcSubscriber<int>($"{Name}.Path.NumWaypoints");
            _pathGetMovementAllowed = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Path.GetMovementAllowed");
            _pathSetMovementAllowed = Svc.PluginInterface.GetIpcSubscriber<bool, object>($"{Name}.Path.SetMovementAllowed");
            _pathGetAlignCamera = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.Path.GetAlignCamera");
            _pathSetAlignCamera = Svc.PluginInterface.GetIpcSubscriber<bool, object>($"{Name}.Path.SetAlignCamera");
            _pathGetTolerance = Svc.PluginInterface.GetIpcSubscriber<float>($"{Name}.Path.GetTolerance");
            _pathSetTolerance = Svc.PluginInterface.GetIpcSubscriber<float, object>($"{Name}.Path.SetTolerance");

            _pathfindAndMoveTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, bool>($"{Name}.SimpleMove.PathfindAndMoveTo");
            _pathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>($"{Name}.SimpleMove.PathfindInProgress");
        }
        catch (Exception ex) { ex.Log(); }
    }

    internal static T? Execute<T>(Func<T> func)
    {
        try
        {
            if (func != null)
                return func();
        }
        catch (Exception ex) { ex.Log(); }

        return default;
    }

    internal static void Execute(Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex) { ex.Log(); }
    }

    internal static void Execute<T>(Action<T> action, T param)
    {
        try
        {
            action?.Invoke(param);
        }
        catch (Exception ex) { ex.Log(); }
    }

    internal static void Execute<T1, T2>(Action<T1, T2> action, T1 p1, T2 p2)
    {
        try
        {
            action?.Invoke(p1, p2);
        }
        catch (Exception ex) { ex.Log(); }
    }

    internal static bool NavIsReady() => Execute(() => _navIsReady!.InvokeFunc());
    internal static float NavBuildProgress() => Execute(() => _navBuildProgress!.InvokeFunc());
    internal static void NavReload() => Execute(() => _navReload!.InvokeFunc());
    internal static void NavRebuild() => Execute(() => _navRebuild!.InvokeFunc());
    internal static Task<List<Vector3>>? NavPathfind(Vector3 from, Vector3 to, bool fly = false) => Execute(() => _navPathfind!.InvokeFunc(from, to, fly));
    internal static bool NavIsAutoLoad() => Execute(() => _navIsAutoLoad!.InvokeFunc());
    internal static void NavSetAutoLoad(bool value) => Execute(_navSetAutoLoad!.InvokeAction, value);

    internal static Vector3? QueryMeshNearestPoint(Vector3 pos, float halfExtentXZ, float halfExtentY) => Execute(() => _queryMeshNearestPoint!.InvokeFunc(pos, halfExtentXZ, halfExtentY));
    internal static Vector3? QueryMeshPointOnFloor(Vector3 pos, bool allowUnlandable, float halfExtentXZ) => Execute(() => _queryMeshPointOnFloor!.InvokeFunc(pos, allowUnlandable, halfExtentXZ));

    internal static void PathMoveTo(List<Vector3> waypoints, bool fly) => Execute(_pathMoveTo!.InvokeAction, waypoints, fly);
    internal static void PathStop() => Execute(_pathStop!.InvokeAction);
    internal static bool PathIsRunning() => Execute(() => _pathIsRunning!.InvokeFunc());
    internal static int PathNumWaypoints() => Execute(() => _pathNumWaypoints!.InvokeFunc());
    internal static bool PathGetMovementAllowed() => Execute(() => _pathGetMovementAllowed!.InvokeFunc());
    internal static void PathSetMovementAllowed(bool value) => Execute(_pathSetMovementAllowed!.InvokeAction, value);
    internal static bool PathGetAlignCamera() => Execute(() => _pathGetAlignCamera!.InvokeFunc());
    internal static void PathSetAlignCamera(bool value) => Execute(_pathSetAlignCamera!.InvokeAction, value);
    internal static float PathGetTolerance() => Execute(() => _pathGetTolerance!.InvokeFunc());
    internal static void PathSetTolerance(float tolerance) => Execute(_pathSetTolerance!.InvokeAction, tolerance);

    internal static void PathfindAndMoveTo(Vector3 pos, bool fly) => Execute(() => _pathfindAndMoveTo!.InvokeFunc(pos, fly));
    internal static bool PathfindInProgress() => Execute(() => _pathfindInProgress!.InvokeFunc());
}

