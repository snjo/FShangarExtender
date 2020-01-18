using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FShangarExtender
{
	public class Debugger
	{

		/// <summary>
		/// an output which can be activated
		/// </summary>
		/// <param name="text"></param>
		/// <param name="debug"></param>
		public static void advancedDebug(string text, bool debug)
		{
			if (debug)
			{
				Debug.Log(string.Format("{0} -- {1}", Constants.debugMarker, text));
			}
		}


		/// <summary>
		/// an output which can be activated
		/// </summary>
		/// <param name="text"></param>
		/// <param name="debug"></param>
		public static void advancedDebug(object text, bool debug)
		{
			if (debug)
			{
				Debug.Log(string.Format("{0} -- {1}", Constants.debugMarker, text.ToString()));
			}
		}
	}
}
