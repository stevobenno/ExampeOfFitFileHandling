


---


# Introduction #
So you've just installed the Garmin Fitness Plugin for [SportTracks](http://www.zonefivesoftware.com/SportTracks)!  Of course the first thing you want to do is configure it to your needs.  This is what the settings page is for.  This page allows you to enable or disable [SportTracks](http://www.zonefivesoftware.com/SportTracks) integration inside the plugin.  This way you can use it exactly as it is done in Garmin Training Center, or you can use the different options that [SportTracks](http://www.zonefivesoftware.com/SportTracks) offers.  Let's see how it's done!

# Accessing the settings #
First of all, you want to access the settings page.  To do so, simply select the "Settings" link in the "Other tasks" panel to the left.

![http://garminworkouts.googlecode.com/files/Settings%20access.gif](http://garminworkouts.googlecode.com/files/Settings%20access.gif)

Once clicked, this link will lead you to the settings page which has a special section for plugins.  In this section, you will find the "Garmin Fitness Settings".  An alternate way of accessing the settings page is to click on the settings button ![http://garminworkouts.googlecode.com/files/Settings%20Button.gif](http://garminworkouts.googlecode.com/files/Settings%20Button.gif) in the upper right corner of the Garmin Fitness view.  Both methods lead you to the following screen :

![http://garminworkouts.googlecode.com/files/Settings%20passive-h.gif](http://garminworkouts.googlecode.com/files/Settings%20passive-h.gif)

# Zone categories #
## Workouts and zone categories ##
When first developing the plugin, I had to make a choice when it came to zones.  On one hand, the different Garmin Fitness devices use some user-defined zones.  On the other hand, [SportTracks](http://www.zonefivesoftware.com/SportTracks) uses it's own user-defined zones.  In the first version of the plugin, only Garmin zones were supported and event then, only for the heart rate target type.  After some discussions with the different early users, it was decided to give the user the choice to use either Garmin or [SportTracks](http://www.zonefivesoftware.com/SportTracks) zones.  This lead to the option in the settings page.  As you can see you have the choice for every type of available target to use either Garmin or [SportTracks](http://www.zonefivesoftware.com/SportTracks) zones by selecting the right combo box.  This affects the list of predefined zones that will be displayed when you define your target for a workout's step.  For instance, if I set the heart rate setting to "Garmin", the list will look as follows

![http://garminworkouts.googlecode.com/files/Garmin%20zones.gif](http://garminworkouts.googlecode.com/files/Garmin%20zones.gif)

If you select the [SportTracks](http://www.zonefivesoftware.com/SportTracks) zone, it will be populated with your own [SportTracks](http://www.zonefivesoftware.com/SportTracks) defined zones.  In my case it looks like this

![http://garminworkouts.googlecode.com/files/ST%20zones.gif](http://garminworkouts.googlecode.com/files/ST%20zones.gif)

## Resetting a step's category ##
Now that you have discovered how to change between Garmin and [SportTracks](http://www.zonefivesoftware.com/SportTracks) zones you might want to change your already created workouts.  when doing so, you will notice that they have kept their old values regardless of the new setting value.  This is to prevent having invalid steps in a workout.  So you might wonder how you can apply the setting to your steps.  This is simple, you must simply choose the "Custom" zone to repopulate the list with the right (Garmin or [SportTracks](http://www.zonefivesoftware.com/SportTracks)) zones.

## Specifying the zone categories ##
In the settings page you will also have noticed that you can select the zone categories to use for cadence and power.  This allows you to select which zone category will be used for the cadence and power target respectively.  Opening the listbox will list all of the zones you have defined for either cadence or power.

![http://garminworkouts.googlecode.com/files/Settings%20expanded%20combo-g.gif](http://garminworkouts.googlecode.com/files/Settings%20expanded%20combo-g.gif)

![http://garminworkouts.googlecode.com/files/Categories%20view.gif](http://garminworkouts.googlecode.com/files/Categories%20view.gif)


For the heart rate and speed targets, the listbox will be populated with the heart rate and speed zone category as defined in the "Activity categories" of the "Categories" view.  As you can see below, I chose to use the "Cycling" heart rate zones and those zones are what appear in the target's listbox..

![http://garminworkouts.googlecode.com/files/Activity%20Categories.gif](http://garminworkouts.googlecode.com/files/Activity%20Categories.gif)

![http://garminworkouts.googlecode.com/files/Cycling%20HR%20Zones.gif](http://garminworkouts.googlecode.com/files/Cycling%20HR%20Zones.gif)

Since I use information contained within the activity category, it is very important to select the right activity category when creating your workouts.  Failure to do so will result in the display of the wrong zones in your target.

You can also notice the reason why the user is given the selection of what cadence and power zone to use.  These options are simply not available in the activity categories.  At the moment, there is no option to select what zone to use on a per-activity category basis.

For the heart rate, there is an extra option only available when you select to use [SportTracks](http://www.zonefivesoftware.com/SportTracks) zones.  This allows you to select whether these targets will be exported as BPM or as a percent of maximum heart rate.  This option is only available in version 1.1.278 or later.

![http://garminworkouts.googlecode.com/files/Settings%20HR%20comboboxes.gif](http://garminworkouts.googlecode.com/files/Settings%20HR%20comboboxes.gif)

A similar option is also available for power so the user can select to export either as %FTP or as watts (from version 1.1.322)

![http://garminworkouts.googlecode.com/files/Settings%20power%20comboboxes.gif](http://garminworkouts.googlecode.com/files/Settings%20power%20comboboxes.gif)


# Allow workouts with more than 20 steps #
This option is also know as the "auto-split" feature.  The different Garmin Fitness units have a maximum of 20 steps per workout.  This limitation can definitely be annoying.  HENNES has found a clean workaround that the plugin implements.  It consists of splitting the workouts with more than 20 steps into multiple workouts that can be executed one after the other.  If you check this checkbox, this feature will be active, if not, the plugin will behave like Garmin Training Center and limit all workouts to 20 steps.

For more information on how to use this feature, head to the [Advanced Operations](http://code.google.com/p/garminworkouts/wiki/AdvancedOperations?ts=1232236954&updated=AdvancedOperations#Workouts_with_more_than_20_steps) section.

# Garmin activity categories #
To integrate the Garmin Workouts Plugin seamlessly into [SportTracks](http://www.zonefivesoftware.com/SportTracks), it was chosen to associate every activity category to a Garmin activity category.  Garmin only provides 3 categories : running, biking and other.  Each of these categories has their own speed and heart rate zones.  The "Biking" category also has power zones.  Since these zones might not match with the zones defined in [SportTracks](http://www.zonefivesoftware.com/SportTracks), you must decide to which sport each [SportTracks](http://www.zonefivesoftware.com/SportTracks) corresponds.  This is what the section is for and it looks like the following :

![http://garminworkouts.googlecode.com/files/Categories%20settings-b.gif](http://garminworkouts.googlecode.com/files/Categories%20settings-b.gif)

By default, all categories are assigned to Garmin's "Other" activity category.  To change this, simply click on the zone you wish to modify.  For instance, if I click on "Running" I get the following :

![http://garminworkouts.googlecode.com/files/Running%20category%20details-c.gif](http://garminworkouts.googlecode.com/files/Running%20category%20details-c.gif)

You notice that it is currently set to inherit it's parent setting which is the following :

![http://garminworkouts.googlecode.com/files/My%20Activities%20category%20details-b.gif](http://garminworkouts.googlecode.com/files/My%20Activities%20category%20details-b.gif)

To change the "Running" activity category setting, you must use the radio buttons to the right.  Since you don't want to inherit the parent's category anymore, start by clicking on the "Use custom category" radio button.  This enables the following three radio buttons for you to select the right one.  Of course it makes sense to choose the "Running" Garmin category.  This gives the following result :

![http://garminworkouts.googlecode.com/files/Running%20category%20modified%20details-c.gif](http://garminworkouts.googlecode.com/files/Running%20category%20modified%20details-c.gif)

You can notice that all the children have been updated to "Running".  That is because they all have the "Use parent category" setting.  If any one of those categories was overriden, it would have kept it's setting.

## Hide in workout list ##
This option allows you to hide certain categories in the workouts list.  for isntance, you might have a category for your workouts on the home trainer.  Since you never have any workouts in the "Racing" category, you mgiht wish to hide it in order to clean up the display.  By checking the "Hide in workout list" checkbox, you will hide the category from the workout list in the main workout page on the plugin.

# Device Communication #
The last section of settings is to configure device communication aspects of the plugin.  It looks like the following :

![http://garminworkouts.googlecode.com/files/Device%20communication.gif](http://garminworkouts.googlecode.com/files/Device%20communication.gif)

## Default export directory ##
This option is used when you select to export your workouts to files.  Rather than asking for the directory at every time you export, the directory entered in this option will automatically be selected, speeding up the export process.

## Export warmup & cooldown as ##
On the newest devices (Edge 500 & 800) two new step intensities have been added, "Warmup" and "Cooldown".  Since these step intensities are not supported by older devices, you have two options to configure if these intensities should be exported as "Active" or "Rest" steps.

Once you are done setting up the plugin, you can move on to the [basic operations](BasicOperations.md).