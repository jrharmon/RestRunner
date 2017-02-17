# REST Runner

A REST client for Windows, written in WPF.  It's main advantages over other clients is simple chaining of values
between calls, and native handling of basic auth credentials across environments.

It is currently focused on JSON-based APIs, but can easily be extended to work better with XML or form-url-encoded ones.

## Table of Contents

  1. [Getting Started](#getting-started)
  1. [Commands](#commands)
  1. [Chains](#chains)
  1. [Multiple Execution](#multiple-execution)
  1. [Categories](#categories)
  1. [Environment Values](#environment-values)
  1. [Results](#results)
  1. [Environments](#environments)
  1. [Globals](#globals)
  1. [Settings](#settings)

## Getting Started

Getting started with REST Runner is easy, but knowing how its interface and features work allow you to work much more efficiently.

## Commands

Commands are the main element of REST Runner, and represent a single REST endpoint, and the data to send and capture from it.  They contain following:

* Settings
  * Name: The label for the command.  This is shown in the comman list on the left of the window.
  * URL: The URL to call.  If there is a category base URL specified, this will be appended after it.
    * If your URL starts with http:// or https:// though, it will override whatever the base URL specifies.
  * Method: The HTTP method to use (GET, POST, PUT, etc.)
  * Credentials: The Basic Auth credentials to use.  These are defined in the environment flyout.
  * Username/Password: If you aren't using stored credentials, you can manually enter the username/password here.
  * Description: Any freeform notes about the command.
* Parameters
  * These are values that are expected to be set by the user before running the command, with the first column being the parameter name, and the second being the value.
  * Putting the parameter name, enclosed in percent symbols, in either the URL or body will cause it to be replaced by the current value. (ex. %CustomerId%)
  * Previous values are stored, so you can easily switch between them.
  * If the value is empty, then it will look for a value from the environment values.
* Capture values
  * These are the values that should be saved from the response of this command, with the first column being the name to save it as, and the second being the JSON path of the value.
  * The path can be period-separated, if you are saving a property of a sub-object.
  * You can specify an array index as well, by simply putting the index where the array is in the path.
  * Examples
    * CustomerId
    * Customer.Address.City
    * Customers.0.FirstName //get FirstName from the first customer in the Customers array
* Headers
  * The headers to send with the command, with the first column being the header name, and the second being the value.
  * Some headers are added automatically.  If you add them manually though, those values will override the defaults, and if you specify a blank value, then the header will not be sent at all.
* Body
  * This is the body of the REST request, which is expected to be JSON formatted.

## Chains

Chaines are a group of commands that are run sequentially with a single click, optionally passing values between calls.  You can pull commands from any category
together, into a single chain.

Each chain has its own default settings that are copied from the category of the first command added.  Any command added afterwards, from a different category, will
have its category defaults moved directly into the command, so that it will still function as before, but won't be tied to the default values of the chain.

## Multiple Execution

With both commands and chains, you can run them multiple times automatically, using the Submit Multiple button, next to the normal Submit button.  You can run them
either:

* Single-threaded: One after the other
* Multi-threaded: Many running at the same time

Any failed will cause execution to stop.

## Categories

Categories group up multiple commands and chains together in the command/chain list on the left of the window.  For commands, they also allow you to set a common
base URL, as well as credential to use for all commands.  Any command can override these defaults, however.

The header of a category has three buttons:

* Expand/Collapse the category
* Add a new command to the category
* Edit the category

## Environment Values

Below the command view, there is a list of environment values.  These are values that will be used as parameter values with the same name, if the parameter has no value
of its own.  These are created/updated automatically when running a command with capture values specified.  They can be set manually, if needed, though.

## Results

Here is where the results of the commands are displayed.  The header display the HTTP response code, then name of the command/chain and the time it took to execute.
There are also three sections, showing the input/request body, response body, and the headers for both the request and response.

Below the results are two buttons to clear the existing ones, and to copy the details of each to the clipboard.  Copying to the clipboard is very helpful if you need
to record the actual requests/responses used when getting a certain response, or to share with others.

## Environments

In the title bar, it displays the current environment.  Clicking on the environment, will open up a flyout, showing all environments, and there settings.
From here, you can select a new environment, and update any settings for it.

* Settings
  * Name: The name of the environment.  The defaults are dev, local, prod, stage and test.
  * Execution Warning Delay: The number of minutes you can be idle within an environment, before it will prompt you before running a new command.  This is useful
  for when you do something in prod, and then come back to the app a while later, forgetting you are still pointing there.
  * Description: Any freeform notes about the environment.
* Variables
  * These are values that are environment-specific, and will be used for any parameter that is not set elsewhere.
* Credentials
  * Named basic auth credentials.  If you have the same credential in multiple environments, the proper one will be used automatically as you switch environments.

## Globals

The globals section, found at the bottom of the flyout, is basically just another environment, but one where all settings/credentials are available, regardless of
the current environment.  Anything in the current environment will override anything from the Globals.

## Settings

Clicking on the settings link with the gear icon in the title bar will bring up the flyout.

It contains the following settings:

* Replace 'localhost'/'127.0.0.1' with machine name: This is useful if you are trying to use Fiddler, as by default, any localhost or 127.0.0.1 URL will bypass
Fiddler.  Replacing it with the machine name will cause it to be visible to Fiddler though.
* Ignore Certificate Errors: If true, using https with a machine that doesn't have a valid certificate will still work.  This is helpful when working with development
machines, as they often don't have their certificates setup.

It also allows importing from a REST Runner or PostMan file, and exporting to a REST Runner file.  Exporting to a PostMan file is not currently supported, but could
be easily added.