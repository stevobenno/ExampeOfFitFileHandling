using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using GarminFitnessPlugin.Data;

namespace GarminFitnessPlugin.Controller
{
    class WorkoutExporter
    {
        public static void ExportWorkout(IWorkout workout, Stream exportStream)
        {
            List<IWorkout> workouts = new List<IWorkout>();

            workouts.Add(workout);

            ExportWorkouts(workouts, exportStream, false);
        }

        public static void ExportWorkouts(List<IWorkout> workouts, Stream exportStream)
        {
            ExportWorkouts(workouts, exportStream, false);
        }

        public static void ExportWorkoutToFIT(IWorkout workout, Stream exportStream)
        {
            List<IWorkout> workouts = new List<IWorkout>();

            workouts.Add(workout);

            ExportWorkoutsToFIT(workouts, exportStream, false);
        }

        public static void ExportWorkoutsToFIT(List<IWorkout> workouts, Stream exportStream)
        {
            ExportWorkoutsToFIT(workouts, exportStream, false);
        }

        private static void ExportWorkouts(List<IWorkout> workouts, Stream exportStream, bool skipExtensions)
        {
            Debug.Assert(exportStream.CanWrite && exportStream.Length == 0);
            XmlDocument document = new XmlDocument();
            XmlNode database;
            XmlAttribute attribute;
            List<IWorkout> concreteWorkouts = new List<IWorkout>();

            document.AppendChild(document.CreateXmlDeclaration("1.0", "UTF-8", "no"));
            database = document.CreateNode(XmlNodeType.Element, "TrainingCenterDatabase", null);
            document.AppendChild(database);
            
            // xmlns namespace attribute
            attribute = document.CreateAttribute("xmlns");
            attribute.Value = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
            database.Attributes.Append(attribute);

            // xmlns:xsi namespace attribute
            attribute = document.CreateAttribute("xmlns", "xsi", Constants.xmlns);
            attribute.Value = Constants.xsins;
            database.Attributes.Append(attribute);

            // xsi:schemaLocation namespace attribute
            attribute = document.CreateAttribute("xsi", "schemaLocation", Constants.xsins);
            attribute.Value = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd";
            database.Attributes.Append(attribute);

            XmlNode workoutsNode = document.CreateNode(XmlNodeType.Element, "Workouts", null);
            database.AppendChild(workoutsNode);

            foreach (IWorkout currentWorkout in workouts)
            {
                if (currentWorkout.GetSplitPartsCount() == 1)
                {
                    concreteWorkouts.Add(currentWorkout);
                }
                else
                {
                    List<WorkoutPart> parts = currentWorkout.SplitInSeperateParts();

                    foreach (WorkoutPart part in parts)
                    {
                        concreteWorkouts.Add(part);
                    }
                }
            }

            foreach (IWorkout concreteWorkout in concreteWorkouts)
            {
                ExportWorkoutInternal(concreteWorkout, document, workoutsNode);
            }

            document.Save(new StreamWriter(exportStream));
        }

        private static void ExportWorkoutsToFIT(List<IWorkout> workouts, Stream exportStream, bool skipExtensions)
        {
        }

        private static void ExportWorkoutInternal(IWorkout workout, XmlDocument document, XmlNode parentNode)
        {
            workout.LastExportDate = DateTime.Now;
            workout.Serialize(parentNode, "Workout", document);
        }

        private static void ExportWorkoutToFITInternal(IWorkout workout, XmlDocument document, XmlNode parentNode)
        {
        }
    }
}
