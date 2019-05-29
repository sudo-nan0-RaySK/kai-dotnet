using System;
using Newtonsoft.Json.Linq;

namespace Kai.Module
{
	[Flags]
	public enum KaiCapabilities
	{
		GestureData,
		LinearFlickData,
		FingerShortcutData,
		FingerPositionalData,
		PYRData,
		QuaternionData,
		AccelerometerData,
		GyroscopeData,
		MagnetometerData
	}

	public enum Gesture
	{
		SwipeUp,
		SwipeDown,
		SwipeLeft,
		SwipeRight,
		SideSwipeUp,
		SideSwipeDown,
		SideSwipeLeft,
		SideSwipeRight,
		Pinch2Begin,
		Pinch2End,
		GrabBegin,
		GrabEnd,
		Pinch3Begin,
		Pinch3End,
		DialBegin,
		DialEnd			
	}

	public enum Hand
	{
		Left,
		Right
	}

	public struct Vector3
	{
		public float x, y, z;
	}

	public struct Quaternion
	{
		public float w, x, y, z;
	}

	public class ActionPerformedTest
	{
		public string MethodLevel;
		public string ModuleID;
		public string ModuleSecret;
		public bool IsInitialised;
		public JObject JsonData;
	}
}