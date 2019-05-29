using System;
using  Kai.Module;
using NUnit;
using NUnit.Framework;

namespace KaiSDKUnitTests
{
	[TestFixture]
	public class KaiSDKUnitTest1
	{
		[Test]
		public void BaseModule_Initialise_GivenModuleIDAndSecret_SetsInitialisedTrue()
		{
			//Arrange
			string ModuleID = "some-module-name";
			string ModuleSecret = "some_very_long_string";
			ActionPerformedTest actionPerformedTest = new ActionPerformedTest();
			
			//Act
			KaiSDK.Initialise(ModuleID,ModuleSecret,actionPerformedTest);
			
			//Assert
			Assert.True(actionPerformedTest.IsInitialised);
			Assert.AreEqual(ModuleID,actionPerformedTest.ModuleID);
			Assert.AreEqual(ModuleSecret,actionPerformedTest.ModuleSecret);
		}
	}
}