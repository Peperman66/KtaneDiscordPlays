﻿using System;
using System.Collections;
using UnityEngine;

public class ProbingComponentSolver : ComponentSolver
{
	public ProbingComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_wires = _component.GetValue<MonoBehaviour[]>("selectables");
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Get the readings with !{0} cycle. Try a combination with !{0} connect 4 3. Cycle reads 1&2, 1&3, 1&4, 1&5, 1&6.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (_wires == null || _wires[0] == null || _wires[1] == null || _wires[2] == null || _wires[3] == null ||
			_wires[4] == null || _wires[5] == null)
			yield break;

		if (split.Length == 1 && split[0] == "cycle")
		{
			yield return "Reading the frequencies";
			yield return EnsureWiresConnected(1, 0);

			for (var i = 1; i < 6; i++)
			{
				yield return ConnectWires(i, 0);
				yield return new WaitForSecondsWithCancel(2.0f, false);
				if (CoroutineCanceller.ShouldCancel) break;
			}
			yield return ConnectWires(4, 4); //Leave the blue wire disconnected.
			yield return "trycancel The probing cycle was cancelled";
			yield break;
		}

		if (split.Length != 3 || split[0] != "connect" ||
			!int.TryParse(split[1], out int red) || !int.TryParse(split[2], out int blue) ||
			red < 1 || red > 6 || blue < 1 || blue > 6 || red == blue)
			yield break;

		yield return "Probing Solve Attempt";
		yield return EnsureWiresConnected(red - 1, blue - 1);

		//Because a strike elsewhere on the bomb may cause this module to strike,
		//about 20% of the time.
		yield return "solve";
		yield return "strike";
	}

	private IEnumerator EnsureWiresConnected(int red, int blue)
	{
		int x = (red + 1) % 6;
		while (x == red || x == blue)
		{
			x++;
			x %= 6;
		}
		int y = (blue + 1) % 6;
		while (y == red || y == blue || y == x)
		{
			y++;
			y %= 6;
		}

		yield return DoInteractionClick(_wires[x]);
		yield return DoInteractionClick(_wires[y]);
		yield return new WaitForSeconds(0.1f);

		yield return DoInteractionClick(_wires[x]);
		yield return DoInteractionClick(_wires[y]);
		yield return new WaitForSeconds(0.1f);

		yield return DoInteractionClick(_wires[red]);
		yield return DoInteractionClick(_wires[blue]);
	}

	private IEnumerator ConnectWires(int red, int blue)
	{
		yield return DoInteractionClick(_wires[red]);
		yield return DoInteractionClick(_wires[blue]);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("bActive"))
			yield return true;
		WireValues[] ansWires = _component.GetValue<WireValues[]>("mWires");
		WireValues ansAWire = _component.GetValue<WireValues>("mTargetWireA");
		WireValues ansBWire = _component.GetValue<WireValues>("mTargetWireB");
		yield return RespondToCommandInternal($"connect {Array.IndexOf(ansWires, ansAWire) + 1} {Array.IndexOf(ansWires, ansBWire) + 1}");
		while (!_component.GetValue<bool>("bSolved"))
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ProbingModule");

	private readonly MonoBehaviour[] _wires;
	private readonly object _component;

	private enum WireValues
	{
		NONE = 0,
		A = 1,
		B = 2,
		C = 4,
		D = 8
	}
}
