


---


# Introduction #
Now that you're all set-up properly as described in the [Settings](Settings.md) page you are ready to create and manage your workouts.  Of course the basic operations are roughly the same as in Garmin Training Center which is creating and deleting workouts, adding and removing steps, changing the order of those steps and of course filling them with the right information.  Keep on reading for instructions on accomplishing those tasks.

# Accessing the Garmin Fitness view #
To access the view available with the Garmin Fitness Plugin you will need to click on the link in the "Select View" menu.

![http://garminworkouts.googlecode.com/files/Garmin%20Fitness%20Access.gif](http://garminworkouts.googlecode.com/files/Garmin%20Fitness%20Access.gif)

Once you have clicked this link, you will be presented the following screen.

![http://garminworkouts.googlecode.com/files/Garmin%20Fitness%20view-c.gif](http://garminworkouts.googlecode.com/files/Garmin%20Fitness%20view-c.gif)

This screen is where you will be able to do everything this plugin allows.

# Managing workouts #

## Creating workouts ##
The first thing you will want to do is to create a workout.  For this purpose, you must first select the category you want to put the workout in.  I have selected My Activites/Cycling/Road but feel free to choose the right category that suits your own workout.  Once done, you must hit the new workout button, in the upper left.  ![http://garminworkouts.googlecode.com/files/New%20Workout%20Button.gif](http://garminworkouts.googlecode.com/files/New%20Workout%20Button.gif)  This creates a workout with a default name and that contains a single step.  The keyboard shortcut CTRL-N also triggers the same action.

![http://garminworkouts.googlecode.com/files/New%20workout-d.gif](http://garminworkouts.googlecode.com/files/New%20workout-d.gif)

## Deleting a workout ##
When a workout is selected in the workouts list, you will notice that the delete workout button is enabled.  Deleting a workout is simple, simply hit the delete button that is also located in the upper left corner ![http://garminworkouts.googlecode.com/files/Delete%20Workout%20Button.gif](http://garminworkouts.googlecode.com/files/Delete%20Workout%20Button.gif)  You can also hit the "delete" key on the keyboard if you prefer.

# Personalizing a workout #

## Changing the workout's name ##
Changing a workout's name is a simple task Simply click in the textbox next to the "Name :" label and enter your desired name.  The name you enter must be unique and cannot be empty

## Adding new steps ##
After changing the workout's name, you will want to add steps to your workout.  To do so, you have two buttons, one to add regular steps ![http://garminworkouts.googlecode.com/files/New%20regular%20step%20button.gif](http://garminworkouts.googlecode.com/files/New%20regular%20step%20button.gif), and one to add repeat steps ![http://garminworkouts.googlecode.com/files/New%20repeat%20step%20button.gif](http://garminworkouts.googlecode.com/files/New%20repeat%20step%20button.gif)  You can also hit the keyboard shortcuts : CTRL-N for a new regular step and CTRL-R for a new repeat.  The devices are limited to 20 steps per workout.  Notice also that when a step is selected, the new step (either regular or repeat) will be added right after the selected step.  When there is no step selected, it will be added as the first step of the workout.

### Repeat steps ###
Repeat steps are a special type of steps.  Basically, a repeat step contains a list of steps (either regular or repeat) that must be repeated a certain number of times before moving forward in the workout.  When creating a repeat step, it will automatically contain a single default step.

This is the result of adding 2 new steps, one regular and one repeat.

![http://garminworkouts.googlecode.com/files/2%20new%20steps-b.gif](http://garminworkouts.googlecode.com/files/2%20new%20steps-b.gif)


## Removing a step ##
Of course every so often you will want to remove a step from your workout.  A very simple button is meant for this purpose and, you guessed it by now, it looks like this ![http://garminworkouts.googlecode.com/files/Delete%20Workout%20Button.gif](http://garminworkouts.googlecode.com/files/Delete%20Workout%20Button.gif).  Here again, the "delete" key on the keyboard will have the same effect.  Having empty workouts is useless so when you delete the last step of a workout, a new default step will automatically replace it.

### Repeat steps ###
Again, repeat steps need some more explications.  First of all, deleting the repeat step will also delete all of it's children, so make sure this is really what you want to do.  Secondly, just like workouts, an empty repeat step makes no sense.  So when you delete the last child inside a repeat step, the repeat step will also be automatically deleted.


## Changing the steps order ##
Next to the creation and deletion buttons, you find two buttons that are used to change the order of the steps inside the workout ![http://garminworkouts.googlecode.com/files/Move%20buttons.gif](http://garminworkouts.googlecode.com/files/Move%20buttons.gif).  The up arrow moves the step up one position inside the workout or inside the repeat step.  Opposingly, the down arrow moves the step down one position.

# Personalizing steps #
Of course all the above means nothing if you don't setup the different steps that your workout contains.  First of all, you can give a custom name to your step.  To do so, you must simply type a name in the textbox right next to the "Custom step name:" label.  You can also specify if the step is active or resting.  By default, the step will be set as being active, checking the box next to the custom name textbox will rectify this situation.

A step is composed of a duration and a target.  A step's duration defines when a certain step will end and the target defines a goal for a step.  Let's see how to define these elements.  Of course you must start by selecting the step you want to edit in the steps list.  This will enable the bottom section of the view as you can see in the following screenshot.

![http://garminworkouts.googlecode.com/files/Steps%20details-c.gif](http://garminworkouts.googlecode.com/files/Steps%20details-c.gif)


## Selecting the step's duration ##
A step's duration is a condition that must be met for the step to end.  You have eight different options for the duration

  1. When you press the 'lap' button on your device
  1. When you go for a certain distance
  1. When you go for a certain amount of time
  1. When your heart rate gets above a certain level
  1. When your heart rate get below a certain level
  1. When you have burned a certain number of calories
  1. When your power gets above a certain level (FIT devices only)
  1. When your power rate get below a certain level (FIT devices only)

To choose the duration you desire, simply click on the drop down list in the "Step Duration" section.

![http://garminworkouts.googlecode.com/files/Duration%20details-b.gif](http://garminworkouts.googlecode.com/files/Duration%20details-b.gif)

Once you have made your selection, the interface will update to allow you to set the parameters you desire.  Depending on the type of duration, you will see one of the following :

| For the 'Lap' button duration, you don't have to specify anything, you just press the 'Lap' button on your device when you wish the step to end. | ![http://garminworkouts.googlecode.com/files/Lap%20duration-b.gif](http://garminworkouts.googlecode.com/files/Lap%20duration-b.gif) |
|:-------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------|
| For a distance duration, you have to specify the desired distance.  Note that even though the unit is in kilometers in the following screenshot, the unit used will be the one specified for the category in which you put the workout. | ![http://garminworkouts.googlecode.com/files/Distance%20duration-b.gif](http://garminworkouts.googlecode.com/files/Distance%20duration-b.gif) |
| The time duration pops up a time control, that allows you to set the time you want for your step | ![http://garminworkouts.googlecode.com/files/Time%20duration-b.gif](http://garminworkouts.googlecode.com/files/Time%20duration-b.gif) |
| For the heart rate above and below you have two choices.  You can either specify the heart rate to acheive in beats per minute (bpm) or in percentage of you maximum heart rate (% Max HR).  The drop down list is used to specify which option you want to use. | ![http://garminworkouts.googlecode.com/files/HR%20duration-b.gif](http://garminworkouts.googlecode.com/files/HR%20duration-b.gif) |
| For the calories burned duration, a simple textbox allows you to set the number of calories desired. | ![http://garminworkouts.googlecode.com/files/Calories%20duration-b.gif](http://garminworkouts.googlecode.com/files/Calories%20duration-b.gif) |
| For the power above and below, you can select whether you want to use watts or as a percentage of funtional threshold power (%FTP) by using a drop down list.  Take note that this option is only available on FIT compatible devices (Edge 500 & 800), which is indicated by the FIT icon ![http://garminworkouts.googlecode.com/files/FIT%20only%20icon.gif](http://garminworkouts.googlecode.com/files/FIT%20only%20icon.gif) in the steps list | ![http://garminworkouts.googlecode.com/files/Power%20duration.gif](http://garminworkouts.googlecode.com/files/Power%20duration.gif) |

### Repeat steps ###
With the introduction of the FIT file format, Garmin also introduced the concept of "Repeat until" steps.  These steps are roughly the same as regular repeat step except that they repeat until you have met a certain condition.  Not surprisingly, the conditions are the same as for regular step duration, which are the following :

  1. Repeat until you go for a certain distance
  1. Repeat until you go for a certain amount of time
  1. Repeat until your heart rate gets above a certain level
  1. Repeat until your heart rate get below a certain level
  1. Repeat until you have burned a certain number of calories
  1. Repeat until your power gets above a certain level
  1. Repeat until your power rate get below a certain level

If you own a FIT compatible device (Edge 500 & 800) you can take advantage of these new "advanced" repeat steps.  You will notice that the steps list reflect that this feature is for FIT compatible devices only by adding an icon ![http://garminworkouts.googlecode.com/files/FIT%20only%20icon.gif](http://garminworkouts.googlecode.com/files/FIT%20only%20icon.gif) before the FIT only step.  The workout is also marked as being FIT only with a special icon ![http://garminworkouts.googlecode.com/files/FIT%20only%20workout%20icon.gif](http://garminworkouts.googlecode.com/files/FIT%20only%20workout%20icon.gif).

The condition for the "repeat until" steps are edited in exactly the same way as a regular step's duration (refer to the table above).

## Selecting the step's target ##
After selecting the right duration for your step, you must also set it's target.  This is done right below the duration portion, where you encounter the following :

![http://garminworkouts.googlecode.com/files/Target%20expanded.gif](http://garminworkouts.googlecode.com/files/Target%20expanded.gif)

Just like the durations, you have multiple target types :

  1. You have no specific target for this step
  1. You want to keep your speed in a certain zone
  1. You want to keep your cadence in a certain zone
  1. You want to keep your heart rate in a certain zone
  1. You want to keep your power in a certain zone (Edge 705 & FIT devices only)

All of the target types use the same model, respectively you can select a predefined zone of define a custom one.  For instance if you choose to keep your cadence in a certain zone you will be offered the following :

![http://garminworkouts.googlecode.com/files/Cadence%20expanded.gif](http://garminworkouts.googlecode.com/files/Cadence%20expanded.gif)

The custom zone corresponds to the first item in the list and all the other options are predefined zones.

### Predefined target zones ###
Each one of the target types have their own predefined zones.  For added versatility, the plugin allows to use the zones already defined in [SportTracks](http://www.zonefivesoftware.com/SportTracks/).  This is what you see in the above screenshot, my cadence zones are "Very low", "Low", "Normal", "High", "Very High" and "Extreme".  You can also use the standard Garmin zones.  To set which zones to use, head to the [Settings](Settings.md) page.

### Custom target zone ###
It happens sometimes that your predefined zones don't correspond to what you want to target in your workout.  This is why the "Custom" option is available and when you select it, you are given text boxes to setup your zone.  Keeping up with the same example, you will get the following for a custom cadence target :

![http://garminworkouts.googlecode.com/files/Cadence%20custom.gif](http://garminworkouts.googlecode.com/files/Cadence%20custom.gif)

As you can see, you can now specify you minimum and maximum cadence for your target.  All the different target types follow the same pattern.

**Note on speed targets** : When setting up a custom speed target, you will be asked to enter the speed in either pace or in units per hour.  The usage of either pace or speed is determined by the category the workout has been put in.  If that category is set to use pace, so will the workout and vice-versa.

## Step notes ##
At the very bottom of the screen you find a second notes text box.  These notes are step notes.  It allows you to add some more information and instructions about your step.  For instance, you could put a note regarding the position to keep on the bike (Standing, seated, aero, etc.)  Note that these informations are an addition that this plugin allows but it is not supported by any device so even though it is exported to the device, it is not available from the unit.

# Exporting/Importing workouts #
To export or import workouts, there is a little panel to the left that allows it.

![http://garminworkouts.googlecode.com/files/Actions.gif](http://garminworkouts.googlecode.com/files/Actions.gif)

When you click on one of the different export/import functions, you are offered the choice of doing the action to/from the device or to/from a file.

![http://garminworkouts.googlecode.com/files/Actions%20clicked.gif](http://garminworkouts.googlecode.com/files/Actions%20clicked.gif)

The export is pretty much straight forward.  If you export to device, it will send the workouts to your device.  If you export to file, you select a folder and it creates one file per workout.

The import is just as easy.  When a new workout is created though you must select it's category.  Since the category determines the units used and if pace or speed is used you must select this category according to your needs.  This is done in the following window :

![http://garminworkouts.googlecode.com/files/Category%20selection.gif](http://garminworkouts.googlecode.com/files/Category%20selection.gif)

It is also possible that you are trying to import a workout which is already present in your logbook.  In that case, you will be asked if you want to replace the old workout or rename the new one.  To rename it, simply type the new name in the textbox at the bottom before clicking on "Rename".  To replace it, simply click on "Replace".

![http://garminworkouts.googlecode.com/files/ReplaceRename.gif](http://garminworkouts.googlecode.com/files/ReplaceRename.gif)



**Notes on export/import to device** : Note that to get the full set of features when exporting and importing, you msut use the latest version of the Garmin Communicator plugin that can be found [here](http://www8.garmin.com/products/communicator/)

# Printing #
With vesion 1.1.322, the ability to print workout was added.  To do so, simply click on the "Print" label in the actions.  You will be presented a page where you can configure your page and preview/print result.  The "Ink saving mode" uses less ink than the "Normal mode" but it's also not as nice on the eye.  Using the preview, you can compare both to make your selection.

## Unroll repeat steps ##
The option "Unroll repeat steps" serves to explode the repeat steps.  What this means is that, if that option is selected, the children steps of a repeat will be duplicated the number of times they should be executed and the repeat step will not be displayed.  This way you can follow the workout step by step with less chances of skipping or duplicating a repetition.