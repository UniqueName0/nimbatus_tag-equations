using BepInEx;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Assets.Nimbatus.Scripts.WorldObjects.Items.Dragging;
using Assets.Nimbatus.Scripts.WorldObjects.Items.DroneParts;
using Assets.Nimbatus.Scripts.WorldObjects.Items.DroneParts.SensorParts;
using Assets.Nimbatus.Scripts.WorldObjects.Items.Selection;
using NCalc;

namespace nimbatus_tag_equations
{
	[BepInProcess("Nimbatus.exe")]
	[BepInPlugin("uniquename.nimbatus.tag-equations", "tag-equations", "0.0.0.0")]
	public class tag_equations : BaseUnityPlugin
	{

		public void Awake()
		{
			var harmony = new Harmony("uniquename.nimbatus.tag-equations");
			harmony.PatchAll();

		}

		public const string modID = "uniquename.nimbatus.tag-equations";
		public const string modName = "tag-equations";
	}


	public class pe
	{
		public static void nis(ref DronePart o)
		{
			// need to read it as a sensor part to get the output key
			SensorPart a = o as SensorPart;
			KeyBinding outKey = Traverse.Create(a).Field("_outputBinding").GetValue() as KeyBinding;
			string newKey = pee(outKey, o);
			outKey.SetKey(newKey);
			
			// the rest is for input keys
			BindableDronePart E = o as BindableDronePart;
			List<KeyBinding> d8k = Traverse.Create(E).Field("KeyBindings").GetValue() as List<KeyBinding>;
			foreach (KeyBinding key in d8k)
			{
				newKey = pee(key, o);
				key.SetKey(newKey);
			}


			//apply to all child parts
			Traverse.Create(E).Field("KeyBindings").SetValue(d8k);

			for (int i = 0; i < o.GetAllChildParts<DronePart>().Count; i++)
			{
				DronePart child = o.GetAllChildParts<DronePart>()[i];
				nis(ref child);
			}
			

		}

		//does the math within the strings
		public static string pee(KeyBinding key, DronePart o)
		{
			string tmp = key.StringCode;
			
			Regex rx = new Regex(@"\[([^\]]+)\]"); //regex to find things within []
			MatchCollection equations = rx.Matches(tmp);

			for (int count = 0; count < equations.Count; count++)
			{
				// probably a better way to do this then a bunch of .Replaces but I'm lazy
				string tmp2 = equations[count].Value
					.Replace("X", o.transform.position.x.ToString())
					.Replace("Y", o.transform.position.y.ToString())
					.Replace("[", "").Replace("]", "").Replace("ABS", "Abs").Replace("ROUND", "Round").Replace("SQRT", "Sqrt");
				// [^A-Z][A-Z]+\( could probably be used to select ABS(), ROUND(), etc. instead of a bunch of .Replaces
				// these have to be replaced because nimbatus makes tags capital which wont work with NCalc

				//does the math within the found strings
				Expression e = new Expression(tmp2);
				var ans = e.Evaluate();
				
				// using Ncalc cause it can do square root, absolute value, and rounding easily
				//alternative way to do this with dataTable
				//DataTable dt = new DataTable();
				//var ans = dt.Compute(tmp2, "");

				//applies answers
				tmp = tmp.Replace(equations[count].Value, ans.ToString());

			}

			return tmp;
		}
	}

	
	[HarmonyPatch(typeof(DronePart), "Update")]
	public class update_Patch
	{
		public static void Prefix(DronePart __instance)
		{
			if (ItemSelector.IsSelected(__instance) && DragAndDropHelper.DraggedItem != __instance && Input.GetKeyUp(KeyCode.K))
			{
				ItemSelector.Deselect(__instance); // deselects it because it wont update what the menu shows
				pe.nis(ref __instance);
			}
		}
	}
}