using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kai.Module
{
	public static partial class KaiSDK
	{
		private static Kai[] connectedKais = new Kai[8];
		private static bool initialised;

		/// <summary>
		/// Represents a boolean value whether the module is authenticated or not
		/// </summary>
		public static bool Authenticated { get; private set; }
		
		/// <summary>
		/// Represents the <code>ProcessName</code> of the process that's in focus
		/// </summary>
		/// <see cref="System.Diagnostics.Process.ProcessName"/>
		public static string ForegroundProcess { get; private set; }
		
		/// <summary>
		/// Represents the module ID
		/// </summary>
		public static string ModuleID { get; private set; }
		
		/// <summary>
		/// Represents the secret data of the Module
		/// </summary>
		public static string ModuleSecret { get; private set; }

		/// <summary>
		/// Occurs when an unrecognised data is received by the module
		/// </summary>
		public static UnknownDataHandler UnknownData;

		/// <summary>
		/// Occurs when an error is reported by the SDK
		/// </summary>
		public static EventHandler<ErrorEventArgs> Error;

		/// <summary>
		/// Contains the value of the default Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultKai { get; } = new Kai();

		/// <summary>
		/// Contains the value of the default left Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultLeftKai { get; } = new Kai();

		/// <summary>
		/// Contains the value of the default right Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultRightKai { get; } = new Kai();
		
		/// <summary>
		/// Throws data when any Kai receives data
		/// </summary>
		public static Kai AnyKai { get; } = new Kai();

		/// <summary>
		/// Initialises the SDK. This function *has* to be called before receiving data from the Kai
		/// </summary>
		public static void Initialise(string moduleId, string moduleSecret)
		{
			ModuleID = moduleId;
			ModuleSecret = moduleSecret;
			
			initialised = true;
		}

		/// <summary>
		/// Connects to the SDK and starts receiving data
		/// </summary>
		/// <exception cref="ApplicationException">Thrown if this function is called before Initialise()</exception>
		public static void Connect()
		{
			if(!initialised)
				throw new ApplicationException("You must call Initialise() before trying to get data");

			SetupConnections();
			
			SendAuth(ModuleID, ModuleSecret);
			
			// TODO check compatibility with SDK
			// Test compatibility
			// Send capabilities
		}
		
		/// <summary>
		/// Sets the Kai's capabilities and subscribes to that data
		/// </summary>
		/// <param name="capabilities">The capabilities to set the Kai to</param>
		/// <param name="kai">The kai to set the capabilities to</param>
		public static void SetCapabilities(Kai kai, KaiCapabilities capabilities)
		{
			if(!Authenticated)
				throw new ApplicationException("module not authenticated");

			var json = new JObject
			{
				[Constants.Type] = Constants.SetCapabilities
			};

			if (capabilities.HasFlag(KaiCapabilities.GestureData))
				json.Add(Constants.GestureData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.LinearFlickData))
				json.Add(Constants.LinearFlickData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.FingerShortcutData))
				json.Add(Constants.FingerShortcutData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.FingerPositionalData))
				json.Add(Constants.FingerPositionalData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.PYRData))
				json.Add(Constants.PYRData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.QuaternionData))
				json.Add(Constants.QuaternionData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.AccelerometerData))
				json.Add(Constants.AccelerometerData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.GyroscopeData))
				json.Add(Constants.GyroscopeData, true);
			
			if (capabilities.HasFlag(KaiCapabilities.MagnetometerData))
				json.Add(Constants.MagnetometerData, true);
			
			Send(json.ToString(Formatting.None));
		}
		
		static partial void SetupConnections();
		static partial void Send(string data);

		private static Task SendAsync(string data)
		{
			return Task.Run(() => Send(data));
		}
		
		private static void SendAuth(string moduleId, string moduleSecret)
		{
			var json = new JObject
			{
				[Constants.Type] = Constants.Authentication,
				[Constants.ModuleId] = moduleId,
				[Constants.ModuleSecret] = moduleSecret
			};

			Send(json.ToString(Formatting.None));
		}

		private static void Handle(string data)
		{
			if (!initialised)
			{
				// TODO Log.Warning($"Received {data} before the listener was initialised. Ignoring...");
				return;
			}

			try
			{
				var input = JObject.Parse(data);

				var success = input[Constants.Success].ToObject<bool>();
				if (success != true)
				{
					DecodeSDKError(input);
					return;
				}

				var type = input[Constants.Type].ToObject<string>();

				switch (type)
				{
					case Constants.Authentication:
						DecodeAuthentication(input);
						break;
					case Constants.IncomingData:
						DecodeIncomingData(input);
						break;
					case Constants.ConnectedKais:
						DecodeConnectedKais(input);
						break;
					default:
						UnknownData?.Invoke(input);
						break;
				}
			}
			catch (Exception)
			{
				// Ignore if the data is not formatted properly
				// TODO Log.Warning($"SDK data not formatted properly. Received: {data}");
			}
		}

		private static void DecodeSDKError(JObject input)
		{
			var errorCode = input[Constants.ErrorCode].ToObject<int>();
			var error = input[Constants.Error].ToObject<string>();
			var message = input[Constants.Message].ToObject<string>();

			Error?.Invoke(null, new ErrorEventArgs(errorCode, error, message));
		}

		private static void DecodeIncomingData(JObject input)
		{
			ForegroundProcess = input[Constants.ForegroundProcess].ToObject<string>();
			var kaiId = input[Constants.KaiId].ToObject<int>();
			var kai = connectedKais[kaiId];
			var defaultKai = input[Constants.DefaultKai].ToObject<bool>();
			var defaultLeftKai = input[Constants.DefaultLeftKai].ToObject<bool>();
			var defaultRightKai = input[Constants.DefaultRightKai].ToObject<bool>();

			var dataList = input[Constants.Data].ToObject<JArray>();

			if (dataList == null)
			{
				// TODO Log.Warning($"SDK data not formatted properly. Received: {data}");
				return;
			}

			foreach (var data in dataList)
			{
				if (data.Type != JTokenType.Object)
				{
					// TODO Log.Warning($"SDK data not formatted properly. Received: {data}");
					continue;
				}

				var dataObject = data.ToObject<JObject>();
				var type = dataObject[Constants.Type].ToObject<string>();

				switch (type)
				{
					case Constants.GestureData:
						ParseGestureData(dataObject);
						break;
					case Constants.FingerShortcutData:
						ParseFingerShortcutData(dataObject);
						break;
					case Constants.PYRData:
						ParsePYRData(dataObject);
						break;
					case Constants.QuaternionData:
						ParseQuaternionData(dataObject);
						break;
					case Constants.LinearFlickData:
						ParseLinearFlickData(dataObject);
						break;
					case Constants.FingerPositionalData:
						ParseFingerPositionalData(dataObject);
						break;
					case Constants.AccelerometerData:
						ParseAccelerometerData(dataObject);
						break;
					case Constants.GyroscopeData:
						ParseGyroscopeData(dataObject);
						break;
					case Constants.MagnetometerData:
						ParseMagnetometerData(dataObject);
						break;
					default:
						UnknownData.Invoke(input);
						break;
				}
			}
			
			void ParseGestureData(JObject data)
			{
				var gesture = data[Constants.Gesture].ToObject<string>();

				if (Enum.TryParse(gesture, true, out Gesture knownGesture))
				{
					FireGestureEvent(new GestureEventArgs(knownGesture));
				}
				else
				{
					FireUnknownGestureEvent(new UnknownGestureEventArgs(gesture));
				}
			}
	
			void ParseFingerShortcutData(JObject data)
			{
				var dataArray = data[Constants.Fingers].ToObject<JArray>();
				var array = new bool[4];
				
				for (var i = 0; i < dataArray.Count; i++)
				{
					array[i] = dataArray[i].ToObject<bool>();
				}
				
				FireFingerShortcutEvent(new FingerShortcutEventArgs(array));
			}
	
			void ParseQuaternionData(JObject data)
			{
				var json = data[Constants.Quaternion].ToObject<JObject>();
				
				var quaternion = new Quaternion
				{
					w = json[Constants.W].ToObject<float>(),
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};
				
				FireQuaternionEvent(new QuaternionEventArgs(quaternion));
			}
	
			void ParsePYRData(JObject json)
			{
				var pitch = json[Constants.Pitch].ToObject<float>();
				var yaw = json[Constants.Yaw].ToObject<float>();
				var roll = json[Constants.Roll].ToObject<float>();
				
				FirePYREvent(new PYREventArgs(pitch, yaw, roll));
			}
			
			void ParseLinearFlickData(JObject data)
			{
				var flick = data[Constants.Flick].ToObject<string>();
				
				FireLinearFlickEvent(new LinearFlickEventArgs(flick));
			}

			void ParseFingerPositionalData(JObject data)
			{
				var json = data[Constants.Fingers].ToObject<JObject>();
				var array = new int[4];
				
				for (var i = 0; i < json.Count; i++)
				{
					array[i] = json[i].ToObject<int>();
				}
				
				FireFingerPositionalEvent(new FingerPositionalEventArgs(array));
			}

			void ParseAccelerometerData(JObject data)
			{
				var json = data[Constants.Accelerometer].ToObject<JObject>();
				
				var accelerometer = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};
				
				FireAccelerometerEvent(new AccelerometerEventArgs(accelerometer));
			}
			
			void ParseGyroscopeData(JObject data)
			{
				var json = data[Constants.Gyroscope].ToObject<JObject>();
				
				var gyroscope = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};
				
				FireGyroscopeEvent(new GyroscopeEventArgs(gyroscope));
			}
			
			void ParseMagnetometerData(JObject data)
			{
				var json = data[Constants.Magnetometer].ToObject<JObject>();
				
				var magnetometer = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};
				
				FireMagnetometerEvent(new MagnetometerEventArgs(magnetometer));
			}
			
			void FireGestureEvent(GestureEventArgs args)
			{
				if(defaultKai)
					DefaultKai.Gesture?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.Gesture?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.Gesture?.Invoke(DefaultRightKai, args);
					
				kai.Gesture?.Invoke(kai, args);
				AnyKai.Gesture?.Invoke(kai, args);
			}
			
			void FireUnknownGestureEvent(UnknownGestureEventArgs args)
			{
				if(defaultKai)
					DefaultKai.UnknownGesture?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.UnknownGesture?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.UnknownGesture?.Invoke(DefaultRightKai, args);
					
				kai.UnknownGesture?.Invoke(kai, args);
				AnyKai.UnknownGesture?.Invoke(kai, args);
			}
			
			void FireLinearFlickEvent(LinearFlickEventArgs args)
			{
				if(defaultKai)
					DefaultKai.LinearFlick?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.LinearFlick?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.LinearFlick?.Invoke(DefaultRightKai, args);
					
				kai.LinearFlick?.Invoke(kai, args);
				AnyKai.LinearFlick?.Invoke(kai, args);
			}
			
			void FireFingerShortcutEvent(FingerShortcutEventArgs args)
			{
				if(defaultKai)
					DefaultKai.FingerShortcut?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.FingerShortcut?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.FingerShortcut?.Invoke(DefaultRightKai, args);
					
				kai.FingerShortcut?.Invoke(kai, args);
				AnyKai.FingerShortcut?.Invoke(kai, args);
			}
			
			void FireFingerPositionalEvent(FingerPositionalEventArgs args)
			{
				if(defaultKai)
					DefaultKai.FingerPositionalData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.FingerPositionalData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.FingerPositionalData?.Invoke(DefaultRightKai, args);
					
				kai.FingerPositionalData?.Invoke(kai, args);
				AnyKai.FingerPositionalData?.Invoke(kai, args);
			}
			
			void FirePYREvent(PYREventArgs args)
			{
				if(defaultKai)
					DefaultKai.PYRData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.PYRData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.PYRData?.Invoke(DefaultRightKai, args);
					
				kai.PYRData?.Invoke(kai, args);
				AnyKai.PYRData?.Invoke(kai, args);
			}
			
			void FireQuaternionEvent(QuaternionEventArgs args)
			{
				if(defaultKai)
					DefaultKai.QuaternionData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.QuaternionData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.QuaternionData?.Invoke(DefaultRightKai, args);
					
				kai.QuaternionData?.Invoke(kai, args);
				AnyKai.QuaternionData?.Invoke(kai, args);
			}
			
			void FireAccelerometerEvent(AccelerometerEventArgs args)
			{
				if(defaultKai)
					DefaultKai.AccelerometerData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.AccelerometerData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.AccelerometerData?.Invoke(DefaultRightKai, args);
					
				kai.AccelerometerData?.Invoke(kai, args);
				AnyKai.AccelerometerData?.Invoke(kai, args);
			}
			
			void FireGyroscopeEvent(GyroscopeEventArgs args)
			{
				if(defaultKai)
					DefaultKai.GyroscopeData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.GyroscopeData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.GyroscopeData?.Invoke(DefaultRightKai, args);
					
				kai.GyroscopeData?.Invoke(kai, args);
				AnyKai.GyroscopeData?.Invoke(kai, args);
			}
			
			void FireMagnetometerEvent(MagnetometerEventArgs args)
			{
				if(defaultKai)
					DefaultKai.MagnetometerData?.Invoke(DefaultKai, args);
				if(defaultLeftKai)
					DefaultLeftKai.MagnetometerData?.Invoke(DefaultLeftKai, args);
				if(defaultRightKai)
					DefaultRightKai.MagnetometerData?.Invoke(DefaultRightKai, args);
					
				kai.MagnetometerData?.Invoke(kai, args);
				AnyKai.MagnetometerData?.Invoke(kai, args);
			}
		}

		private static void DecodeAuthentication(JObject input)
		{
			Authenticated = input[Constants.Authenticated].ToObject<bool>();
		}

		private static void DecodeConnectedKais(JObject input)
		{
			var kaiList = input[Constants.Kais].ToObject<JArray>();
			connectedKais = new Kai[8];
			foreach (var token in kaiList)
			{
				var kai = token.ToObject<JObject>();
				var kaiID = kai[Constants.KaiId].ToObject<int>();
				var hand = kai[Constants.Hand].ToObject<string>();
				var defaultKai = kai[Constants.DefaultKai]?.ToObject<bool>();
				var defaultLeftKai = kai[Constants.DefaultKai]?.ToObject<bool>();
				var defaultRightKai = kai[Constants.DefaultKai]?.ToObject<bool>();

				if (!Enum.TryParse(hand, true, out Hand handEnum))
					handEnum = Hand.Left;

				if (defaultKai == true)
				{
					DefaultKai.KaiID = kaiID;
					DefaultKai.Hand = handEnum;
				}

				if (defaultLeftKai == true)
				{
					DefaultLeftKai.KaiID = kaiID;
					DefaultLeftKai.Hand = Hand.Left;
				}

				if (defaultRightKai == true)
				{
					DefaultRightKai.KaiID = kaiID;
					DefaultRightKai.Hand = Hand.Right;
				}
				
				connectedKais[kaiID] = new Kai
				{
					KaiID = kaiID,
					Hand = handEnum
				};
			}
		}
	}
}