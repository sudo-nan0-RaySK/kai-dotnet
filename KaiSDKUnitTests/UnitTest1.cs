using System;
using  Kai.Module;
using Xunit;

namespace KaiSDKUnitTests
{
	public class KaiSDKUnitTest1
	{
		[Fact]
		public void Initialise_GivenModuleIDAndSecret_SetsInitialisedTrue()
		{
			//Arrange
			string ModuleID = "some-module-name";
			string ModuleSecret = "some_very_long_string";
			ActionPerformedTest actionPerformedTest = new ActionPerformedTest();
			
			//Act
			KaiSDK.Initialise(ModuleID,ModuleSecret,actionPerformedTest);
			
			//Arrange
			Assert.True(actionPerformedTest.IsInitialised);
			Assert.Equal(ModuleID,actionPerformedTest.ModuleID);
			Assert.Equal(ModuleSecret,actionPerformedTest.ModuleSecret);
		}
	}
}