# Management Scripts

These scripts help manage building/deploying REST Runner.  To use them, you must create a Powershell script called
InitializeEnvironmentVariables.ps1 that sets the required sessions environment variables.  The required variables are:

* publishDir - The directory path to publish a ClickOnce installer to, as well as a portable version

Example:

    New-Item -Name publishDir -Path env: -Value "\\myserver\deployables\RestRunner\" | out-null
