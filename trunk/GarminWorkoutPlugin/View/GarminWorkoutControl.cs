using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Windows.Forms;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Data.Measurement;
using ZoneFiveSoftware.Common.Visuals;
using ZoneFiveSoftware.Common.Visuals.Fitness;
using GarminWorkoutPlugin.Data;
using GarminWorkoutPlugin.Controller;

namespace GarminWorkoutPlugin.View
{
    partial class GarminWorkoutControl : UserControl, IGarminPluginControl
    {
        public GarminWorkoutControl()
        {
            InitializeComponent();

            WorkoutsList.RowDataRenderer = new WorkoutRowDataRenderer(WorkoutsList);
            WorkoutsList.LabelProvider = new WorkoutIconLabelProvider();

            StepsList.RowDataRenderer = new StepRowDataRenderer(StepsList);
            StepsList.LabelProvider = new StepIconLabelProvider();

            m_DurationPanels = new ZoneFiveSoftware.Common.Visuals.Panel[]
            {
                null,
                DistanceDurationPanel,
                TimeDurationPanel,
                HeartRateDurationPanel,
                HeartRateDurationPanel,
                CaloriesDurationPanel
            };
        }

#region UI Callbacks

        protected override void OnPaint(PaintEventArgs e)
        {
            if (PaintEnabled)
            {
                base.OnPaint(e);
            }
        }

        private void GarminWorkoutControl_Load(object sender, EventArgs e)
        {
            PluginMain.GetApplication().Calendar.SelectedChanged += new EventHandler(OnCalendarSelectedChanged);
        }

        void OnCalendarSelectedChanged(object sender, EventArgs e)
        {
            m_SelectedWorkouts.Clear();
            // Find the workouts planned on the selected date
            for (int i = 0; i < WorkoutManager.Instance.Workouts.Count; ++i)
            {
                Workout currentWorkout = WorkoutManager.Instance.Workouts[i];

                if(currentWorkout.ScheduledDates.Contains(PluginMain.GetApplication().Calendar.Selected))
                {
                    m_SelectedWorkouts.Add(currentWorkout);
                }
            }

            // Select them
            SelectWorkoutsInList(m_SelectedWorkouts);
            WorkoutCalendar.SelectedDate = PluginMain.GetApplication().Calendar.Selected;
        }

        private void GarminWorkoutControl_SizeChanged(object sender, EventArgs e)
        {
            BuildWorkoutsList();
            UpdateUIFromWorkout(SelectedWorkout);

            // Reset splitter distances
            CategoriesSplit.SplitterDistance = Options.CategoriesPanelSplitSize;
            WorkoutSplit.SplitterDistance = Options.WorkoutPanelSplitSize;
            StepsNotesSplitter.SplitterDistance = Options.StepNotesSplitSize;
            StepSplit.SplitterDistance = Math.Max(StepSplit.Panel1MinSize, StepSplit.Height - Options.StepPanelSplitSize);
            CalendarSplit.SplitterDistance = Math.Max(CalendarSplit.Panel1MinSize, CalendarSplit.Height - Options.CalendarPanelSplitSize);
        }

        private void CategoriesSplit_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            Options.CategoriesPanelSplitSize = e.SplitX;
        }

        private void WorkoutSplit_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            Options.WorkoutPanelSplitSize = e.SplitX;
        }

        private void StepSplit_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            Options.StepPanelSplitSize = StepSplit.Height - e.SplitY;
        }

        private void CalendarSplit_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            Options.CalendarPanelSplitSize = CalendarSplit.Height - e.SplitY;
        }

        private void DurationComboBox_SelectionChangedCommited(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            IDuration.DurationType newType = (IDuration.DurationType)DurationComboBox.SelectedIndex;

            if (concreteStep.Duration.Type != newType)
            {
                concreteStep.Duration = DurationFactory.Create((IDuration.DurationType)DurationComboBox.SelectedIndex, concreteStep);

                UpdateUIFromStep(SelectedStep);
                StepsList.Invalidate();

                Utils.SaveWorkoutsToLogbook();
            }
        }

        private void TargetComboBox_SelectionChangedCommited(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            ITarget.TargetType newType = (ITarget.TargetType)TargetComboBox.SelectedIndex;

            if (concreteStep.Target.Type != newType)
            {
                concreteStep.Target = TargetFactory.Create((ITarget.TargetType)TargetComboBox.SelectedIndex, concreteStep);

                UpdateUIFromStep(SelectedStep);
                StepsList.Invalidate();

                Utils.SaveWorkoutsToLogbook();
            }
        }

        private void DistanceDurationPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (SelectedWorkout != null)
            {
                DistanceDurationUnitsLabel.Text = Length.LabelAbbr(SelectedWorkout.Category.DistanceUnits);
            }
        }

        private void NewWorkoutButton_Click(object sender, EventArgs e)
        {
            AddNewWorkout();
            Utils.SaveWorkoutsToLogbook();
        }

        private void RemoveWorkoutButton_Click(object sender, EventArgs e)
        {
            DeleteSelectedWorkouts();
        }

        private void ScheduleWorkoutButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_SelectedWorkouts.Count; ++i)
            {
                if(!m_SelectedWorkouts[i].ScheduledDates.Contains(WorkoutCalendar.SelectedDate))
                {
                    m_SelectedWorkouts[i].ScheduledDates.Add(WorkoutCalendar.SelectedDate);
                }
            }

            RefreshCalendarView();
            UpdateUIFromWorkout(m_SelectedWorkouts);
            Utils.SaveWorkoutsToLogbook();
        }

        private void RemoveScheduledDateButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_SelectedWorkouts.Count; ++i)
            {
                m_SelectedWorkouts[i].ScheduledDates.Remove(WorkoutCalendar.SelectedDate);
            }

            RefreshCalendarView();
            UpdateUIFromWorkout(m_SelectedWorkouts);
            Utils.SaveWorkoutsToLogbook();
        }

        private void WorkoutCalendar_SelectionChanged(object sender, DateTime newSelection)
        {
            bool hasScheduledDate = false;
            bool areAllWorkoutsScheduledOnDate = true;

            // Highlight scheduled dates
            for (int i = 0; i < m_SelectedWorkouts.Count; ++i)
            {
                Workout currentWorkout = m_SelectedWorkouts[i];
                bool foundSelectedDatePlanned = false;

                for (int j = 0; j < currentWorkout.ScheduledDates.Count; ++j)
                {
                    if (newSelection == currentWorkout.ScheduledDates[j])
                    {
                        foundSelectedDatePlanned = true;
                        hasScheduledDate = true;
                    }
                }

                if (!foundSelectedDatePlanned)
                {
                    areAllWorkoutsScheduledOnDate = false;
                }
            }

            ScheduleWorkoutButton.Enabled = m_SelectedWorkouts.Count > 0 && newSelection >= DateTime.Today && !areAllWorkoutsScheduledOnDate;
            RemoveScheduledDateButton.Enabled = hasScheduledDate;
        }

        private void StepsList_SelectedChanged(object sender, EventArgs e)
        {
            m_CancelledStepSelection = false;

            if (StepsList.Selected.Count > 0)
            {
                if (StepsList.Selected.Count == 1)
                {
                    IStep selectedStep = (IStep)((StepWrapper)StepsList.Selected[0]).Element;

                    if (!m_SelectedSteps.Contains(selectedStep))
                    {
                        SelectedStep = selectedStep;
                    }
                    else
                    {
                        SelectStepsInList(m_SelectedSteps);

                        m_CancelledStepSelection = true;
                        m_SelectedStepCancelled = selectedStep;
                    }
                }
                else
                {
                    // We have multiple items selected
                    m_SelectedSteps.Clear();
                    for (int i = 0; i < StepsList.Selected.Count; ++i)
                    {
                        m_SelectedSteps.Add((IStep)((StepWrapper)StepsList.Selected[i]).Element);
                    }

                    m_SelectedSteps.Sort(SelectedStepComparison);
                }
            }
            else
            {
                SelectedStep = null;
            }

            RefreshActions();
            UpdateUIFromStep(m_SelectedSteps);
        }
        
        private void WorkoutsList_SelectedChanged(object sender, EventArgs e)
        {
            m_CancelledWorkoutSelection = false;

            if (WorkoutsList.Selected.Count > 0)
            {
                if (WorkoutsList.Selected.Count == 1)
                {
                    if (WorkoutsList.Selected[0].GetType() == typeof(ActivityCategoryWrapper))
                    {
                        SelectedWorkout = null;
                        m_SelectedCategory = (IActivityCategory)((ActivityCategoryWrapper)WorkoutsList.Selected[0]).Element;
                    }
                    else if (WorkoutsList.Selected[0].GetType() == typeof(WorkoutWrapper))
                    {
                        Workout selectedWorkout = (Workout)((WorkoutWrapper)WorkoutsList.Selected[0]).Element;

                        if (!m_SelectedWorkouts.Contains(selectedWorkout))
                        {
                            SelectedWorkout = selectedWorkout;
                            m_SelectedCategory = SelectedWorkout.Category;
                        }
                        else
                        {
                            SelectWorkoutsInList(m_SelectedWorkouts);

                            m_CancelledWorkoutSelection = true;
                            m_SelectedWorkoutCancelled = selectedWorkout;
                        }
                    }
                    else
                    {
                        Trace.Assert(false);
                    }
                }
                else
                {
                    // We have multiple items selected, keep only the workouts
                    m_SelectedWorkouts.Clear();
                    for (int i = 0; i < WorkoutsList.Selected.Count; ++i)
                    {
                        if (WorkoutsList.Selected[i].GetType() == typeof(WorkoutWrapper))
                        {
                            m_SelectedWorkouts.Add((Workout)((WorkoutWrapper)WorkoutsList.Selected[i]).Element);
                        }
                    }
                    m_SelectedCategory = null;
                }
            }
            else
            {
                SelectedWorkout = null;
                m_SelectedCategory = null;
            }

            RefreshActions();
            UpdateUIFromWorkout(m_SelectedWorkouts);
        }

        private void StepNameText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            Trace.Assert(StepNameText.Text.Length <= 15);
            RegularStep concreteStep = (RegularStep)SelectedStep;

            concreteStep.Name = StepNameText.Text;
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void RestingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;

            concreteStep.IsRestingStep = RestingCheckBox.Checked;

            Utils.SaveWorkoutsToLogbook();
        }

        private void CaloriesDurationText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && concreteStep.Duration.Type == IDuration.DurationType.Calories);
            CaloriesDuration concreteDuration = (CaloriesDuration)concreteStep.Duration;

            if (Utils.IsTextIntegerInRange(CaloriesDurationText.Text, 1, 65535))
            {
                e.Cancel = false;
            }
            else
            {
                MessageBox.Show(String.Format(m_ResourceManager.GetString("IntegerRangeValidationText"), 1, 65535),
                                m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                System.Media.SystemSounds.Asterisk.Play();
                CaloriesDurationText.Text = concreteDuration.CaloriesToSpend.ToString();
                e.Cancel = true;
            }
        }

        private void CaloriesDurationText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && concreteStep.Duration.Type == IDuration.DurationType.Calories);
            CaloriesDuration concreteDuration = (CaloriesDuration)concreteStep.Duration;

            concreteDuration.CaloriesToSpend = UInt16.Parse(CaloriesDurationText.Text);
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void CaloriesDurationText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void HeartRateReferenceComboBox_SelectionChangedCommited(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null);

            if (concreteStep.Duration.Type == IDuration.DurationType.HeartRateAbove)
            {
                HeartRateAboveDuration concreteDuration = (HeartRateAboveDuration)concreteStep.Duration;
                bool isPercentMax = HeartRateReferenceComboBox.SelectedIndex == 1;

                if (isPercentMax && concreteDuration.MaxHeartRate > 100)
                {
                    concreteDuration.MaxHeartRate = 100;
                    HeartRateDurationText.Text = "100";
                }
                else if (!isPercentMax && concreteDuration.MaxHeartRate < 30)
                {
                    concreteDuration.MaxHeartRate = 30;
                    HeartRateDurationText.Text = "30";
                }

                concreteDuration.IsPercentageMaxHeartRate = isPercentMax;
            }
            else if (concreteStep.Duration.Type == IDuration.DurationType.HeartRateBelow)
            {
                HeartRateBelowDuration concreteDuration = (HeartRateBelowDuration)concreteStep.Duration;
                bool isPercentMax = HeartRateReferenceComboBox.SelectedIndex == 1;

                if (isPercentMax && concreteDuration.MinHeartRate > 100)
                {
                    concreteDuration.MinHeartRate = 100;
                    HeartRateDurationText.Text = "100";
                }
                else if (!isPercentMax && concreteDuration.MinHeartRate < 30)
                {
                    concreteDuration.MinHeartRate = 30;
                    HeartRateDurationText.Text = "30";
                }

                concreteDuration.IsPercentageMaxHeartRate = isPercentMax;
            }
            else
            {
                Trace.Assert(false);
            }

            StepsList.Invalidate();
        }

        private void HeartRateDurationText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && (concreteStep.Duration.Type == IDuration.DurationType.HeartRateAbove || concreteStep.Duration.Type == IDuration.DurationType.HeartRateBelow));
            bool isPercentMax;
            UInt16 HRValue;

            if (concreteStep.Duration.Type == IDuration.DurationType.HeartRateAbove)
            {
                HeartRateAboveDuration concreteDuration = (HeartRateAboveDuration)concreteStep.Duration;

                isPercentMax = concreteDuration.IsPercentageMaxHeartRate;
                HRValue = concreteDuration.MaxHeartRate;
            }
            else
            {
                HeartRateBelowDuration concreteDuration = (HeartRateBelowDuration)concreteStep.Duration;

                isPercentMax = concreteDuration.IsPercentageMaxHeartRate;
                HRValue = concreteDuration.MinHeartRate;
            }

            if (isPercentMax)
            {
                if (Utils.IsTextIntegerInRange(HeartRateDurationText.Text, 1, 100))
                {
                    e.Cancel = false;
                }
                else
                {
                    MessageBox.Show(String.Format(m_ResourceManager.GetString("IntegerRangeValidationText"), 1, 100),
                                    m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    System.Media.SystemSounds.Asterisk.Play();
                    HeartRateDurationText.Text = HRValue.ToString();
                    e.Cancel = true;
                }
            }
            else
            {
                if (Utils.IsTextIntegerInRange(HeartRateDurationText.Text, 30, 240))
                {
                    e.Cancel = false;
                }
                else
                {
                    MessageBox.Show(String.Format(m_ResourceManager.GetString("IntegerRangeValidationText"), 30, 240),
                                    m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    System.Media.SystemSounds.Asterisk.Play();
                    HeartRateDurationText.Text = HRValue.ToString();
                    e.Cancel = true;
                }
            }
        }

        private void HeartRateDurationText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && (concreteStep.Duration.Type == IDuration.DurationType.HeartRateAbove || concreteStep.Duration.Type == IDuration.DurationType.HeartRateBelow));

            if (concreteStep.Duration.Type == IDuration.DurationType.HeartRateAbove)
            {
                HeartRateAboveDuration concreteDuration = (HeartRateAboveDuration)concreteStep.Duration;

                concreteDuration.MaxHeartRate = Byte.Parse(HeartRateDurationText.Text);
            }
            else if (concreteStep.Duration.Type == IDuration.DurationType.HeartRateBelow)
            {
                HeartRateBelowDuration concreteDuration = (HeartRateBelowDuration)concreteStep.Duration;

                concreteDuration.MinHeartRate = Byte.Parse(HeartRateDurationText.Text);
            }
            else
            {
                Trace.Assert(false);
            }

            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void HeartRateDurationText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void DistanceDurationText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && concreteStep.Duration.Type == IDuration.DurationType.Distance);
            DistanceDuration concreteDuration = (DistanceDuration)concreteStep.Duration;
            float minDistance, maxDistance;

            if (SelectedWorkout.Category.DistanceUnits == Length.Units.Mile)
            {
                minDistance = 0.01f;
                maxDistance = 40.00f;
            }
            else if (SelectedWorkout.Category.DistanceUnits == Length.Units.Kilometer)
            {
                minDistance = 0.01f;
                maxDistance = 65.00f;
            }
            else
            {
                minDistance = 1.0f;
                maxDistance = 65000.0f;
            }

            if (Utils.IsTextFloatInRange(DistanceDurationText.Text, minDistance, maxDistance))
            {
                e.Cancel = false;
            }
            else
            {
                MessageBox.Show(String.Format(m_ResourceManager.GetString("DoubleRangeValidationText"), minDistance, maxDistance),
                                m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                System.Media.SystemSounds.Asterisk.Play();
                DistanceDurationText.Text = String.Format("{0:0.00}", concreteDuration.GetDistanceInUnits(SelectedWorkout.Category.DistanceUnits));
                e.Cancel = true;
            }
        }

        private void DistanceDurationText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && concreteStep.Duration.Type == IDuration.DurationType.Distance);
            DistanceDuration concreteDuration = (DistanceDuration)concreteStep.Duration;

            concreteDuration.SetDistanceInUnits(float.Parse(DistanceDurationText.Text), SelectedWorkout.Category.DistanceUnits);
            DistanceDurationText.Text = String.Format("{0:0.00}", concreteDuration.GetDistanceInUnits(SelectedWorkout.Category.DistanceUnits));
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void DistanceDurationText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void TimeDurationUpDown_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Duration != null && concreteStep.Duration.Type == IDuration.DurationType.Time);
            TimeDuration concreteDuration = (TimeDuration)concreteStep.Duration;

            concreteDuration.TimeInSeconds = TimeDurationUpDown.Duration;
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void TimeDurationUpDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void ZoneComboBox_SelectionChangedCommited(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type != ITarget.TargetType.Null);
            int selectedIndex = ZoneComboBox.SelectedIndex;

            switch (concreteStep.Target.Type)
            {
                case ITarget.TargetType.HeartRate:
                    {
                        UpdateHeartRateTargetFromComboBox(concreteStep, selectedIndex);
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        UpdateCadenceTargetFromComboBox(concreteStep, selectedIndex);
                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        UpdateSpeedTargetFromComboBox(concreteStep, selectedIndex);
                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        UpdatePowerTargetFromComboBox(concreteStep, selectedIndex);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            ZoneComboBox.SelectedIndex = selectedIndex;
            TargetDirtyPictureBox.Visible = concreteStep.Target.IsDirty;
            StepsList.Invalidate();
            WorkoutsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void LowRangeTargetText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type != ITarget.TargetType.Null);

            UInt16 intMin = 0;
            UInt16 intMax = 0;
            double doubleMin = 0;
            double doubleMax = 0;
            string oldValue = "";
            RangeValidationInputType inputType = RangeValidationInputType.Integer;

            switch(concreteStep.Target.Type)
            {
                case ITarget.TargetType.HeartRate:
                    {
                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range);
                        HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MinHeartRate.ToString();
                        inputType = RangeValidationInputType.Integer;

                        if (concreteTarget.IsPercentageMaxHeartRate)
                        {
                            intMin = 1;
                            intMax = 100;
                        }
                        else
                        {
                            intMin = 30;
                            intMax = 240;
                        }
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range);
                        CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MinCadence.ToString();
                        intMin = 0;
                        intMax = 254;
                        inputType = RangeValidationInputType.Integer;

                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range);
                        SpeedRangeTarget concreteTarget = (SpeedRangeTarget)baseTarget.ConcreteTarget;

                        if (concreteTarget.ViewAsPace)
                        {
                            double paceTime = concreteTarget.GetMaxSpeedInMinutesPerUnit(SelectedWorkout.Category.DistanceUnits);
                            UInt16 minutes, seconds;

                            Utils.FloatToTime(paceTime, out minutes, out seconds);
                            oldValue = String.Format("{0:00}:{1:00}", minutes, seconds);
                            doubleMin = 1.0 / (Length.Convert(60, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits) / Constants.MinutesPerHour);
                            doubleMax = 1.0 / (Length.Convert(1.0002, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits) / Constants.MinutesPerHour);
                            inputType = RangeValidationInputType.Time;
                        }
                        else
                        {
                            oldValue = String.Format("{0:0.00}", concreteTarget.GetMinSpeedInUnitsPerHour(SelectedWorkout.Category.DistanceUnits));
                            doubleMin = Length.Convert(1, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits);
                            doubleMax = Length.Convert(60, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits);
                            inputType = RangeValidationInputType.Float;
                        }

                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range);
                        PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MinPower.ToString();
                        intMin = 20;
                        intMax = 999;
                        inputType = RangeValidationInputType.Integer;

                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }

            switch(inputType)
            {
                case RangeValidationInputType.Integer:
                    {
                        if (Utils.IsTextIntegerInRange(LowRangeTargetText.Text, intMin, intMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("IntegerRangeValidationText"), intMin, intMax),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            LowRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
                case RangeValidationInputType.Float:
                    {
                        if (Utils.IsTextFloatInRange(LowRangeTargetText.Text, doubleMin, doubleMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("DoubleRangeValidationText"), doubleMin, doubleMax),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            LowRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
                case RangeValidationInputType.Time:
                    {
                        if (Utils.IsTextTimeInRange(LowRangeTargetText.Text, doubleMin, doubleMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            UInt16 minMinutes, minSeconds;
                            UInt16 maxMinutes, maxSeconds;

                            Utils.FloatToTime(doubleMin, out minMinutes, out minSeconds);
                            Utils.FloatToTime(doubleMax, out maxMinutes, out maxSeconds);
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("TimeRangeValidationText"),
                                                          minMinutes, minSeconds,
                                                          maxMinutes, maxSeconds),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            LowRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
            }
        }

        private void LowRangeTargetText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type != ITarget.TargetType.Null);
            bool forceSelectHighTargetText = false;

            switch (concreteStep.Target.Type)
            {
                case ITarget.TargetType.HeartRate:
                    {
                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range);
                        HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;
                        Byte newValue = Byte.Parse(LowRangeTargetText.Text);

                        if (newValue <= concreteTarget.MaxHeartRate)
                        {
                            concreteTarget.MinHeartRate = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue, concreteTarget.IsPercentageMaxHeartRate);
                            forceSelectHighTargetText = true;
                        }
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range);
                        CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;
                        Byte newValue = Byte.Parse(LowRangeTargetText.Text);

                        if (newValue < concreteTarget.MaxCadence)
                        {
                            concreteTarget.MinCadence = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue);
                            forceSelectHighTargetText = true;
                        }
                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range);
                        SpeedRangeTarget concreteTarget = (SpeedRangeTarget)baseTarget.ConcreteTarget;

                        if (concreteTarget.ViewAsPace)
                        {
                            double time = Utils.TimeToFloat(LowRangeTargetText.Text);

                            if (time <= concreteTarget.GetMinSpeedInMinutesPerUnit(SelectedWorkout.Category.DistanceUnits))
                            {
                                concreteTarget.SetMaxSpeedInMinutesPerUnit(time, SelectedWorkout.Category.DistanceUnits);
                            }
                            else
                            {
                                concreteTarget.SetRangeInMinutesPerUnit(time, time, SelectedWorkout.Category.DistanceUnits);
                                forceSelectHighTargetText = true;
                            }
                        }
                        else
                        {
                            double newValue = double.Parse(LowRangeTargetText.Text);

                            if (newValue <= concreteTarget.GetMaxSpeedInUnitsPerHour(SelectedWorkout.Category.DistanceUnits))
                            {
                                concreteTarget.SetMinSpeedInUnitsPerHour(newValue, SelectedWorkout.Category.DistanceUnits);
                            }
                            else
                            {
                                concreteTarget.SetRangeInUnitsPerHour(newValue, newValue, SelectedWorkout.Category.DistanceUnits);
                                forceSelectHighTargetText = true;
                            }
                        }
                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range);
                        PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;
                        UInt16 newValue = UInt16.Parse(LowRangeTargetText.Text);

                        if (newValue < concreteTarget.MaxPower)
                        {
                            concreteTarget.MinPower = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue);
                            forceSelectHighTargetText = true;
                        }
                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }

            StepsList.Invalidate();
            UpdateUIFromStep(SelectedStep);
            Utils.SaveWorkoutsToLogbook();

            if (forceSelectHighTargetText)
            {
                HighRangeTargetText.Focus();
                HighRangeTargetText.SelectAll();
            }
        }

        private void LowRangeTargetText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void HighRangeTargetText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type != ITarget.TargetType.Null);

            UInt16 intMin = 0;
            UInt16 intMax = 0;
            double doubleMin = 0;
            double doubleMax = 0;
            string oldValue = "";
            RangeValidationInputType inputType = RangeValidationInputType.Integer;

            switch (concreteStep.Target.Type)
            {
                case ITarget.TargetType.HeartRate:
                    {
                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range);
                        HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MaxHeartRate.ToString();
                        inputType = RangeValidationInputType.Integer;

                        if (concreteTarget.IsPercentageMaxHeartRate)
                        {
                            intMin = 1;
                            intMax = 100;
                        }
                        else
                        {
                            intMin = 30;
                            intMax = 240;
                        }
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range);
                        CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MaxCadence.ToString();
                        intMin = 0;
                        intMax = 254;
                        inputType = RangeValidationInputType.Integer;

                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range);
                        SpeedRangeTarget concreteTarget = (SpeedRangeTarget)baseTarget.ConcreteTarget;

                        if (concreteTarget.ViewAsPace)
                        {
                            double paceTime = concreteTarget.GetMinSpeedInMinutesPerUnit(SelectedWorkout.Category.DistanceUnits);
                            UInt16 minutes, seconds;

                            Utils.FloatToTime(paceTime, out minutes, out seconds);
                            oldValue = String.Format("{0:00}:{1:00}", minutes, seconds);
                            doubleMin = 1.0 / (Length.Convert(60, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits) / Constants.MinutesPerHour);
                            doubleMax = 1.0 / (Length.Convert(1.0002, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits) / Constants.MinutesPerHour);
                            inputType = RangeValidationInputType.Time;
                        }
                        else
                        {
                            oldValue = String.Format("{0:0.00}", concreteTarget.GetMaxSpeedInUnitsPerHour(SelectedWorkout.Category.DistanceUnits));
                            doubleMin = Length.Convert(1, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits);
                            doubleMax = Length.Convert(60, Length.Units.Mile, SelectedWorkout.Category.DistanceUnits);
                            inputType = RangeValidationInputType.Float;
                        }

                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range);
                        PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;

                        oldValue = concreteTarget.MaxPower.ToString();
                        intMin = 20;
                        intMax = 999;
                        inputType = RangeValidationInputType.Integer;

                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }

            switch(inputType)
            {
                case RangeValidationInputType.Integer:
                    {
                        if (Utils.IsTextIntegerInRange(HighRangeTargetText.Text, intMin, intMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("IntegerRangeValidationText"), intMin, intMax),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            HighRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
                case RangeValidationInputType.Float:
                    {
                        if (Utils.IsTextFloatInRange(HighRangeTargetText.Text, doubleMin, doubleMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("DoubleRangeValidationText"), doubleMin, doubleMax),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            HighRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
                case RangeValidationInputType.Time:
                    {
                        if (Utils.IsTextTimeInRange(HighRangeTargetText.Text, doubleMin, doubleMax))
                        {
                            e.Cancel = false;
                        }
                        else
                        {
                            UInt16 minMinutes, minSeconds;
                            UInt16 maxMinutes, maxSeconds;

                            Utils.FloatToTime(doubleMin, out minMinutes, out minSeconds);
                            Utils.FloatToTime(doubleMax, out maxMinutes, out maxSeconds);
                            MessageBox.Show(String.Format(m_ResourceManager.GetString("TimeRangeValidationText"),
                                                          minMinutes, minSeconds,
                                                          maxMinutes, maxSeconds),
                                            m_ResourceManager.GetString("ValueValidationTitleText"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            System.Media.SystemSounds.Asterisk.Play();
                            HighRangeTargetText.Text = oldValue;
                            e.Cancel = true;
                        }
                        break;
                    }
            }
        }

        private void HighRangeTargetText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type != ITarget.TargetType.Null);
            bool forceSelectLowTargetText = false;

            switch (concreteStep.Target.Type)
            {
                case ITarget.TargetType.HeartRate:
                    {
                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range);
                        HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;
                        Byte newValue = Byte.Parse(HighRangeTargetText.Text);

                        if (newValue >= concreteTarget.MinHeartRate)
                        {
                            concreteTarget.MaxHeartRate = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue, concreteTarget.IsPercentageMaxHeartRate);
                            forceSelectLowTargetText = true;
                        }
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range);
                        CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;
                        Byte newValue = Byte.Parse(HighRangeTargetText.Text);

                        if (newValue >= concreteTarget.MinCadence)
                        {
                            concreteTarget.MaxCadence = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue);
                            forceSelectLowTargetText = true;
                        }
                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range);
                        SpeedRangeTarget concreteTarget = (SpeedRangeTarget)baseTarget.ConcreteTarget;

                        if (concreteTarget.ViewAsPace)
                        {
                            float time = Utils.TimeToFloat(HighRangeTargetText.Text);

                            if (time >= concreteTarget.GetMaxSpeedInMinutesPerUnit(SelectedWorkout.Category.DistanceUnits))
                            {
                                concreteTarget.SetMinSpeedInMinutesPerUnit(time, SelectedWorkout.Category.DistanceUnits);
                            }
                            else
                            {
                                concreteTarget.SetRangeInMinutesPerUnit(time, time, SelectedWorkout.Category.DistanceUnits);
                                forceSelectLowTargetText = true;
                            }
                        }
                        else
                        {
                            double newValue = double.Parse(HighRangeTargetText.Text);

                            if (newValue >= concreteTarget.GetMinSpeedInUnitsPerHour(SelectedWorkout.Category.DistanceUnits))
                            {
                                concreteTarget.SetMaxSpeedInUnitsPerHour(newValue, SelectedWorkout.Category.DistanceUnits);
                            }
                            else
                            {
                                concreteTarget.SetRangeInUnitsPerHour(newValue, newValue, SelectedWorkout.Category.DistanceUnits);
                                forceSelectLowTargetText = true;
                            }
                        }
                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;
                        Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range);
                        PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;
                        UInt16 newValue = UInt16.Parse(HighRangeTargetText.Text);

                        if (newValue > concreteTarget.MinPower)
                        {
                            concreteTarget.MaxPower = newValue;
                        }
                        else
                        {
                            concreteTarget.SetValues(newValue, newValue);
                            forceSelectLowTargetText = true;
                        }
                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }

            StepsList.Invalidate();
            UpdateUIFromStep(SelectedStep);
            Utils.SaveWorkoutsToLogbook();

            if (forceSelectLowTargetText)
            {
                LowRangeTargetText.Focus();
                LowRangeTargetText.SelectAll();
            }
        }

        private void HighRangeTargetText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void WorkoutsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && m_SelectedWorkouts.Count > 0)
            {
                DeleteSelectedWorkouts();
            }
            else if (e.Control)
            {
                if (e.KeyCode == Keys.C || e.KeyCode == Keys.X)
                {
                    // Copy data to clipboard
                    MemoryStream workoutsData = new MemoryStream();

                    // Number of workouts to deserialize
                    workoutsData.WriteByte((Byte)m_SelectedWorkouts.Count);
                    for (int i = 0; i < m_SelectedWorkouts.Count; ++i)
                    {
                        m_SelectedWorkouts[i].Serialize(workoutsData);
                    }

                    Clipboard.SetData("GWP_WorkoutsList", workoutsData);

                    if (e.KeyCode == Keys.X)
                    {
                        // Cut, so remove from workout
                        DeleteSelectedWorkouts();
                    }
                }
                else if (e.KeyCode == Keys.V)
                {
                    if (Clipboard.ContainsData("GWP_WorkoutsList"))
                    {
                        MemoryStream pasteResult = (MemoryStream)Clipboard.GetData("GWP_WorkoutsList");

                        if (SelectedCategory == null)
                        {
                            SelectedCategory = PluginMain.GetApplication().Logbook.ActivityCategories[0];
                        }

                        if (pasteResult != null)
                        {
                            // Set back to start
                            pasteResult.Seek(0, SeekOrigin.Begin);

                            List<Workout> workoutsPasted = new List<Workout>();
                            byte[] intBuffer = new byte[sizeof(Int32)];
                            Byte workoutCount = (Byte)pasteResult.ReadByte();

                            for (int i = 0; i < workoutCount; i++)
                            {
                                Workout newWorkout = WorkoutManager.Instance.CreateWorkout(SelectedCategory);

                                newWorkout.Deserialize(pasteResult, Constants.CurrentVersion);
                                newWorkout.Category = SelectedCategory;
                                workoutsPasted.Add(newWorkout);

                                // We must update the name to avoid duplicates
                                string tempName = newWorkout.Name;

                                if (!Utils.IsTextInteger(tempName))
                                {
                                    // Remove all trailing numbers
                                    while (tempName.LastIndexOfAny("0123456789".ToCharArray()) == tempName.Length - 1)
                                    {
                                        tempName = tempName.Substring(0, tempName.Length - 1);
                                    }
                                }

                                newWorkout.Name = WorkoutManager.Instance.GetUniqueName(tempName);
                            }

                            m_SelectedWorkouts = workoutsPasted;
                            BuildWorkoutsList();
                            UpdateUIFromWorkout(SelectedWorkouts);
                            Utils.SaveWorkoutsToLogbook();
                        }
                    }
                }
            }
        }

        private void WorkoutsList_DragDrop(object sender, DragEventArgs e)
        {
            TreeList.RowHitState type;
            Point mouseLocation = new Point(e.X, e.Y);
            List<Workout> workoutsToMove = (List<Workout>)e.Data.GetData(typeof(List<Workout>));
            object item = WorkoutsList.RowHitTest(WorkoutsList.PointToClient(mouseLocation), out type);

            if (item != null && workoutsToMove != null)
            {
                IActivityCategory category = null;

                if (item.GetType() == typeof(WorkoutWrapper))
                {
                    WorkoutWrapper wrapper = (WorkoutWrapper)item;

                    category = ((Workout)wrapper.Element).Category;
                }
                else if (item.GetType() == typeof(ActivityCategoryWrapper))
                {
                    ActivityCategoryWrapper wrapper = (ActivityCategoryWrapper)item;

                    category = (IActivityCategory)wrapper.Element;
                }
                else
                {
                    // What is this doing here?
                    Trace.Assert(false);
                }

                if (e.Effect == DragDropEffects.Copy)
                {
                    for (int i = 0; i < workoutsToMove.Count; ++i)
                    {
                        workoutsToMove[i] = workoutsToMove[i].Clone();

                        // We must update the name to avoid duplicates
                        string tempName = workoutsToMove[i].Name;

                        if (!Utils.IsTextInteger(tempName))
                        {
                            // Remove all trailing numbers
                            while (tempName.LastIndexOfAny("0123456789".ToCharArray()) == tempName.Length - 1)
                            {
                                tempName = tempName.Substring(0, tempName.Length - 1);
                            }
                        }

                        workoutsToMove[i].Name = WorkoutManager.Instance.GetUniqueName(tempName);
                    }

                }

                for (int i = 0; i < workoutsToMove.Count; ++i)
                {
                    Workout workoutToMove = workoutsToMove[i];

                    if (e.Effect == DragDropEffects.Copy || workoutToMove.Category != category)
                    {
                         workoutToMove.Category = category;
                    }
                }

                m_SelectedWorkouts = workoutsToMove;
                Utils.SaveWorkoutsToLogbook();
                SelectedSteps.Clear();
                BuildWorkoutsList();
                UpdateUIFromWorkout(m_SelectedWorkouts);
            }

            m_IsMouseDownInWorkoutsList = false;
        }

        private void WorkoutsList_DragOver(object sender, DragEventArgs e)
        {
            List<Workout> workoutsToMove = (List<Workout>)e.Data.GetData(typeof(List<Workout>));

            if (workoutsToMove != null)
            {
                if ((e.KeyState & CTRL_KEY_CODE) != 0)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void WorkoutsList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TreeList.RowHitState type;
                object futureSelection = WorkoutsList.RowHitTest(e.Location, out type);

                if (futureSelection != null)
                {
                    if (futureSelection.GetType() == typeof(WorkoutWrapper))
                    {
                        m_IsMouseDownInWorkoutsList = true;
                        m_MouseMovedPixels = 0;
                        m_LastMouseDownLocation = e.Location;
                    }
                }
            }
        }

        private void WorkoutsList_MouseUp(object sender, MouseEventArgs e)
        {
            m_IsMouseDownInWorkoutsList = false;

            if (m_CancelledWorkoutSelection)
            {
                m_SelectedWorkouts.Clear();
                m_SelectedWorkouts.Add(m_SelectedWorkoutCancelled);

                SelectWorkoutsInList(m_SelectedWorkouts);
            }
        }

        private void WorkoutsList_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsMouseDownInWorkoutsList && m_MouseMovedPixels < 5)
            {
                m_MouseMovedPixels += Math.Abs(m_LastMouseDownLocation.X - e.X);
                m_MouseMovedPixels += Math.Abs(m_LastMouseDownLocation.Y - e.Y);

                if (m_MouseMovedPixels >= 5)
                {
                    // Start drag & drop operation
                    DoDragDrop(m_SelectedWorkouts, DragDropEffects.Move | DragDropEffects.Copy);
                }
            }
        }

        private void StepsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && m_SelectedSteps.Count > 0)
            {
                DeleteSelectedSteps();
            }
            else if (e.Control)
            {
                if (e.KeyCode == Keys.C || e.KeyCode == Keys.X)
                {
                    // Copy data to clipboard
                    MemoryStream stepsData = new MemoryStream();
                    List<IStep> baseSteps;

                    baseSteps = GetMinimalStepsBase(m_SelectedSteps);
                    // Number of steps to deserialize
                    stepsData.WriteByte((Byte)baseSteps.Count);
                    for (int i = 0; i < baseSteps.Count; ++i)
                    {
                        baseSteps[i].Serialize(stepsData);
                    }

                    Clipboard.SetData("GWP_StepsList", stepsData);

                    if (e.KeyCode == Keys.X)
                    {
                        // Cut, so remove from workout
                        DeleteSelectedSteps();
                    }
                }
                else if (e.KeyCode == Keys.V)
                {
                    if (Clipboard.ContainsData("GWP_StepsList"))
                    {
                        MemoryStream pasteResult = (MemoryStream)Clipboard.GetData("GWP_StepsList");

                        if (pasteResult != null && SelectedWorkout != null)
                        {
                            // Set back to start
                            pasteResult.Seek(0, SeekOrigin.Begin);

                            List<IStep> stepsPasted = new List<IStep>();
                            byte[] intBuffer = new byte[sizeof(Int32)];
                            Byte stepCount = (Byte)pasteResult.ReadByte();

                            for (int i = 0; i < stepCount; i++)
                            {
                                IStep.StepType type;

                                pasteResult.Read(intBuffer, 0, sizeof(Int32));
                                type = (IStep.StepType)BitConverter.ToInt32(intBuffer, 0);

                                if (type == IStep.StepType.Regular)
                                {
                                    stepsPasted.Add(new RegularStep(pasteResult, Constants.CurrentVersion, SelectedWorkout));
                                }
                                else
                                {
                                    stepsPasted.Add(new RepeatStep(pasteResult, Constants.CurrentVersion, SelectedWorkout));
                                }
                            }

                            // Now that we deserialized, paste in the current workout
                            SelectedWorkout.Steps.AddRange(stepsPasted);

                            UpdateUIFromWorkout(SelectedWorkout, stepsPasted);
                            Utils.SaveWorkoutsToLogbook();
                        }
                    }
                }
            }
        }

        private void StepsList_DragDrop(object sender, DragEventArgs e)
        {
            int rowNumber;
            bool isInUpperHalf;
            object destination;
            UInt16 index;
            Point clientPoint = StepsList.PointToClient(new Point(e.X, e.Y));
            IStep destinationStep;
            List<IStep> parentList;
            List<IStep> newSelection = new List<IStep>();
            List<IStep> stepsToMove = (List<IStep>)e.Data.GetData(typeof(List<IStep>));
            StepRowDataRenderer renderer = (StepRowDataRenderer)StepsList.RowDataRenderer;

            destination = renderer.RowHitTest(clientPoint, out rowNumber, out isInUpperHalf);

            if (destination == null)
            {
                // Insert as the last item in the workout
                destinationStep = SelectedWorkout.Steps[SelectedWorkout.Steps.Count - 1];
                isInUpperHalf = false;
            }
            else
            {
                destinationStep = (IStep)((StepWrapper)destination).Element;
            }
            Utils.GetStepInfo(destinationStep, SelectedWorkout.Steps, out parentList, out index);

            if (!isInUpperHalf)
            {
                index++;
            }

            // Add new items
            for (int i = 0; i < stepsToMove.Count; ++i)
            {
                IStep stepToMove = stepsToMove[i];
                IStep movedStep = stepToMove.Clone();

                parentList.Insert(index, movedStep);
                newSelection.Add(movedStep);

                // This causes the subsequent steps to be appended to this one
                //  This way they keep their original order
                index++;
            }

            // Remove the old ones
            if (e.Effect == DragDropEffects.Move)
            {
                for (int i = 0; i < stepsToMove.Count; ++i)
                {
                    IStep stepToMove = stepsToMove[i];
                    UInt16 srcIndex;
                    Utils.GetStepInfo(stepToMove, SelectedWorkout.Steps, out parentList, out srcIndex);

                    parentList.RemoveAt(srcIndex);
                }
            }

            CleanUpWorkoutAfterDelete(SelectedWorkout);
            Utils.SaveWorkoutsToLogbook();
            UpdateUIFromWorkout(SelectedWorkout, newSelection);

            m_IsMouseDownInStepsList = false;
            renderer.IsInDrag = false;
        }

        private void StepsList_DragOver(object sender, DragEventArgs e)
        {
            bool isCtrlKeyDown = (e.KeyState & CTRL_KEY_CODE) != 0;
            List<IStep> stepsToMove = (List<IStep>)e.Data.GetData(typeof(List<IStep>));

            e.Effect = DragDropEffects.None;

            if (stepsToMove != null)
            {
                StepRowDataRenderer renderer = (StepRowDataRenderer)StepsList.RowDataRenderer;
                Point mouseLocation = StepsList.PointToClient(new Point(e.X, e.Y));
                object item = renderer.RowHitTest(mouseLocation);
                IStep destinationStep;
                int stepsDragged = 0;

                if (item == null)
                {
                    // Insert as the last item in the workout
                    destinationStep = SelectedWorkout.Steps[SelectedWorkout.Steps.Count - 1];
                }
                else
                {
                    destinationStep = (IStep)((StepWrapper)item).Element;
                }

                // We need to count the number of items being moved
                for (int i = 0; i < stepsToMove.Count; ++i)
                {
                    stepsDragged += stepsToMove[i].GetStepCount();
                }

                if (!isCtrlKeyDown ||
                    (isCtrlKeyDown && SelectedWorkout.GetStepCount() + stepsDragged <= 20))
                {
                    // Make sure we are not moving a repeat inside itself
                    if (!isCtrlKeyDown)
                    {
                        for (int i = 0; i < stepsToMove.Count; ++i)
                        {
                            if (stepsToMove[i].Type == IStep.StepType.Repeat)
                            {
                                RepeatStep repeatStep = (RepeatStep)stepsToMove[i];

                                if (repeatStep.IsChildStep(destinationStep))
                                {
                                    return;
                                }
                            }
                        }
                    }

                    renderer.IsInDrag = true;
                    renderer.DragOverClientPosition = mouseLocation;
                    StepsList.Invalidate();

                    if (isCtrlKeyDown)
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                }
            }
        }

        private void StepsList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TreeList.RowHitState type;
                object futureSelection = StepsList.RowHitTest(e.Location, out type);

                if (futureSelection != null)
                {
                    if (futureSelection.GetType() == typeof(StepWrapper))
                    {
                        m_IsMouseDownInStepsList = true;
                        m_MouseMovedPixels = 0;
                        m_LastMouseDownLocation = e.Location;
                    }
                }
            }
        }

        private void StepsList_MouseUp(object sender, MouseEventArgs e)
        {
            StepRowDataRenderer renderer = (StepRowDataRenderer)StepsList.RowDataRenderer;

            m_IsMouseDownInStepsList = false;
            renderer.IsInDrag = false;

            if (m_CancelledStepSelection)
            {
                m_SelectedSteps.Clear();
                m_SelectedSteps.Add(m_SelectedStepCancelled);

                SelectStepsInList(m_SelectedSteps);
            }
        }

        private void StepsList_MouseMove(object sender, MouseEventArgs e)
        {
            if (StepsList.SelectedItems.Count == 0)
            {
                m_IsMouseDownInStepsList = false;
                return;
            }

            if (m_IsMouseDownInStepsList && m_MouseMovedPixels < 5)
            {
                m_MouseMovedPixels += Math.Abs(m_LastMouseDownLocation.X - e.X);
                m_MouseMovedPixels += Math.Abs(m_LastMouseDownLocation.Y - e.Y);

                if (m_MouseMovedPixels >= 5)
                {
                    // Start drag & drop operation
                    DoDragDrop(GetMinimalStepsBase(m_SelectedSteps), DragDropEffects.Move | DragDropEffects.Copy);
                }
            }
        }

        private void RepetitionCountText_Validating(object sender, CancelEventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Repeat);
            RepeatStep concreteStep = (RepeatStep)SelectedStep;

            if (Utils.IsTextIntegerInRange(RepetitionCountText.Text, 2, 99))
            {
                e.Cancel = false;
            }
            else
            {
                MessageBox.Show("Value must be an integer number between 2 and 99", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                System.Media.SystemSounds.Asterisk.Play();
                RepetitionCountText.Text = concreteStep.RepetitionCount.ToString();
                e.Cancel = true;
            }
        }

        private void RepetitionCountText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Repeat);
            RepeatStep concreteStep = (RepeatStep)SelectedStep;

            concreteStep.RepetitionCount = Byte.Parse(RepetitionCountText.Text);
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void RepetitionCountText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void NotesText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedWorkout != null);

            SelectedWorkout.Notes = WorkoutNotesText.Text;

            Utils.SaveWorkoutsToLogbook();
        }


        private void StepNotesText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null);

            SelectedStep.Notes = StepNotesText.Text;

            Utils.SaveWorkoutsToLogbook();
        }

        private void WorkoutNameText_Validating(object sender, CancelEventArgs e)
        {
            Workout workoutWithSameName = WorkoutManager.Instance.GetWorkoutWithName(WorkoutNameText.Text);

            if (WorkoutNameText.Text == String.Empty || (workoutWithSameName != null && workoutWithSameName != SelectedWorkout))
            {
                e.Cancel = true;

                MessageBox.Show(m_ResourceManager.GetString("InvalidWorkoutNameText", m_CurrentCulture),
                                m_ResourceManager.GetString("ErrorText", m_CurrentCulture),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WorkoutNameText_Validated(object sender, EventArgs e)
        {
            Trace.Assert(SelectedWorkout != null);

            SelectedWorkout.Name = WorkoutNameText.Text;
            WorkoutsList.Invalidate();

            BuildWorkoutsList();
            Utils.SaveWorkoutsToLogbook();
        }

        private void WorkoutNameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }

        private void HRRangeReferenceComboBox_SelectionChangedCommited(object sender, EventArgs e)
        {
            Trace.Assert(SelectedStep != null && SelectedStep.Type == IStep.StepType.Regular);
            RegularStep concreteStep = (RegularStep)SelectedStep;
            Trace.Assert(concreteStep.Target != null && concreteStep.Target.Type == ITarget.TargetType.HeartRate);
            BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;
            Trace.Assert(baseTarget.ConcreteTarget != null && baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range);
            HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;
            bool isPercentMax = HRRangeReferenceComboBox.SelectedIndex == 1;
            Byte newMin = concreteTarget.MinHeartRate;
            Byte newMax = concreteTarget.MaxHeartRate;

            if (isPercentMax && newMin > 100)
            {
                newMin = 100;
            }
            else if (!isPercentMax && newMin < 30)
            {
                newMin = 30;
            }

            if (isPercentMax && newMax > 100)
            {
                newMax = 100;
            }
            else if (!isPercentMax && newMax < 30)
            {
                newMax = 30;
            }

            LowRangeTargetText.Text = newMin.ToString();
            HighRangeTargetText.Text = newMax.ToString();

            concreteTarget.SetValues(newMin, newMax, isPercentMax);
            StepsList.Invalidate();

            Utils.SaveWorkoutsToLogbook();
        }

        private void AddStepButton_Click(object sender, EventArgs e)
        {
            AddNewStep(new RegularStep(SelectedWorkout));

            Utils.SaveWorkoutsToLogbook();
        }

        private void AddRepeatButton_Click(object sender, EventArgs e)
        {
            AddNewStep(new RepeatStep(SelectedWorkout));

            Utils.SaveWorkoutsToLogbook();
        }

        private void RemoveItemButton_Click(object sender, EventArgs e)
        {
            DeleteSelectedSteps();
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            Trace.Assert(SelectedWorkout != null && SelectedStep != null);
            UInt16 selectedPosition = 0;
            List<IStep> selectedList = null;

            if (Utils.GetStepInfo(SelectedStep, SelectedWorkout.Steps, out selectedList, out selectedPosition))
            {
                Trace.Assert(selectedPosition > 0);

                selectedList.Reverse(selectedPosition - 1, 2);
                UpdateUIFromWorkout(SelectedWorkout, selectedList[selectedPosition - 1]);

                Utils.SaveWorkoutsToLogbook();
            }
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            Trace.Assert(SelectedWorkout != null && SelectedStep != null);
            UInt16 selectedPosition = 0;
            List<IStep> selectedList = null;

            if (Utils.GetStepInfo(SelectedStep, SelectedWorkout.Steps, out selectedList, out selectedPosition))
            {
                Trace.Assert(selectedPosition < selectedList.Count - 1);

                selectedList.Reverse(selectedPosition, 2);
                UpdateUIFromWorkout(SelectedWorkout, selectedList[selectedPosition + 1]);

                Utils.SaveWorkoutsToLogbook();
            }
        }

        private void splitContainer9_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            Options.StepNotesSplitSize = e.SplitY;
        }

#endregion

        public void ThemeChanged(ITheme visualTheme)
        {
            m_CurrentTheme = visualTheme;

            CategoriesBanner.ThemeChanged(visualTheme);
            WorkoutsList.ThemeChanged(visualTheme);
            DetailsBanner.ThemeChanged(visualTheme);
            StepDetailsBanner.ThemeChanged(visualTheme);
            ScheduleBanner.ThemeChanged(visualTheme);

            StepsList.ThemeChanged(visualTheme);
            WorkoutCalendar.ThemeChanged(visualTheme);
            StepSplit.Panel1.BackColor = visualTheme.Control;
        }

        public void UICultureChanged(System.Globalization.CultureInfo culture)
        {
            m_CurrentCulture = culture;

            StepDescriptionStringFormatter.ResourceManager = m_ResourceManager;
            StepDescriptionStringFormatter.CurrentCulture = m_CurrentCulture;

            UpdateUIStrings();
            BuildWorkoutsList();
            UpdateUIFromWorkout(SelectedWorkout);
        }

        public void RefreshUIFromLogbook()
        {
            m_SelectedCategory = null;
            SelectedWorkout = null;
            SelectedSteps.Clear();

            BuildWorkoutsList();
            UpdateUIFromWorkout(SelectedWorkout);
            RefreshCalendarView();
        }

        private void AddCategoryNode(ActivityCategoryWrapper categoryNode, ActivityCategoryWrapper parent)
        {
            IActivityCategory category = (IActivityCategory)categoryNode.Element;

            if (parent != null)
            {
                parent.Children.Add(categoryNode);
            }

            for (int i = 0; i < category.SubCategories.Count; ++i)
            {
                IActivityCategory currentCategory = category.SubCategories[i];
                ActivityCategoryWrapper newNode = new ActivityCategoryWrapper(categoryNode, currentCategory);

                AddCategoryNode(newNode, categoryNode);
            }
        }

        private void UpdateUIStrings()
        {
            CategoriesBanner.Text = m_ResourceManager.GetString("CategoriesText", m_CurrentCulture);
            DetailsBanner.Text = m_ResourceManager.GetString("DetailsText", m_CurrentCulture);
            StepDetailsBanner.Text = m_ResourceManager.GetString("StepDetailsText", m_CurrentCulture);
            ScheduleBanner.Text = m_ResourceManager.GetString("ScheduleBannerText", m_CurrentCulture);

            NameLabel.Text = m_ResourceManager.GetString("NameLabelText", m_CurrentCulture);
            WorkoutNotesLabel.Text = m_ResourceManager.GetString("NotesLabelText", m_CurrentCulture);
            StepNotesLabel.Text = m_ResourceManager.GetString("NotesLabelText", m_CurrentCulture);
            StepNameLabel.Text = m_ResourceManager.GetString("StepNameLabelText", m_CurrentCulture);
            RestingCheckBox.Text = m_ResourceManager.GetString("RestingCheckBoxText", m_CurrentCulture);
            StepDurationGroup.Text = m_ResourceManager.GetString("StepDurationGroupText", m_CurrentCulture);
            StepDurationLabel.Text = m_ResourceManager.GetString("StepDurationLabelText", m_CurrentCulture);
            StepTargetGroup.Text = m_ResourceManager.GetString("StepTargetGroupText", m_CurrentCulture);
            StepTargetLabel.Text = m_ResourceManager.GetString("StepTargetLabelText", m_CurrentCulture);
            CaloriesDurationLabel.Text = m_ResourceManager.GetString("CaloriesDurationLabelText", m_CurrentCulture);
            DistanceDurationLabel.Text = m_ResourceManager.GetString("DistanceDurationLabelText", m_CurrentCulture);
            HeartRateDurationLabel.Text = m_ResourceManager.GetString("HeartRateDurationLabelText", m_CurrentCulture);
            TimeDurationLabel.Text = m_ResourceManager.GetString("TimeDurationLabelText", m_CurrentCulture);
            ZoneLabel.Text = m_ResourceManager.GetString("WhichZoneText", m_CurrentCulture);
            LowRangeTargetLabel.Text = m_ResourceManager.GetString("BetweenText", m_CurrentCulture);
            MiddleRangeTargetLabel.Text = m_ResourceManager.GetString("AndText", m_CurrentCulture);
            RepetitionCountLabel.Text = m_ResourceManager.GetString("RepetitionCountLabelText", m_CurrentCulture);
            ExportDateTextLabel.Text = m_ResourceManager.GetString("LastExportDateText", m_CurrentCulture);

            // Update duration heart rate reference combo box text
            HeartRateReferenceComboBox.Items.Clear();
            HeartRateReferenceComboBox.Items.Add(CommonResources.Text.LabelBPM);
            HeartRateReferenceComboBox.Items.Add(CommonResources.Text.LabelPercentOfMax);

            // Update target heart rate reference combo box text
            HRRangeReferenceComboBox.Items.Clear();
            HRRangeReferenceComboBox.Items.Add(CommonResources.Text.LabelBPM);
            HRRangeReferenceComboBox.Items.Add(CommonResources.Text.LabelPercentOfMax);

            // Update duration combo box
            int currentSelection = DurationComboBox.SelectedIndex;
            DurationComboBox.Items.Clear();
            for (int i = 0; i < (int)IDuration.DurationType.DurationTypeCount; ++i)
            {
                IDuration.DurationType currentDuration = (IDuration.DurationType)i;
                FieldInfo durationFieldInfo = currentDuration.GetType().GetField(Enum.GetName(currentDuration.GetType(), currentDuration));
                ComboBoxStringProviderAttribute providerAttribute = (ComboBoxStringProviderAttribute)Attribute.GetCustomAttribute(durationFieldInfo, typeof(ComboBoxStringProviderAttribute));

                DurationComboBox.Items.Add(m_ResourceManager.GetString(providerAttribute.StringName, m_CurrentCulture));

                if (currentSelection == i)
                {
                    DurationComboBox.Text = m_ResourceManager.GetString(providerAttribute.StringName, m_CurrentCulture);
                }
            }

            // Update target combo box
            currentSelection = TargetComboBox.SelectedIndex;
            TargetComboBox.Items.Clear();
            for (int i = 0; i < (int)ITarget.TargetType.TargetTypeCount; ++i)
            {
                ITarget.TargetType currentTarget = (ITarget.TargetType)i;
                FieldInfo targetFieldInfo = currentTarget.GetType().GetField(Enum.GetName(currentTarget.GetType(), currentTarget));
                ComboBoxStringProviderAttribute providerAttribute = (ComboBoxStringProviderAttribute)Attribute.GetCustomAttribute(targetFieldInfo, typeof(ComboBoxStringProviderAttribute));

                TargetComboBox.Items.Add(m_ResourceManager.GetString(providerAttribute.StringName, m_CurrentCulture));

                if (currentSelection == i)
                {
                    TargetComboBox.Text = m_ResourceManager.GetString(providerAttribute.StringName, m_CurrentCulture);
                }
            }
        }

        private void UpdateDurationPanelVisibility(IDuration duration)
        {
            if (m_CurrentDurationPanel != null)
            {
                m_CurrentDurationPanel.Visible = false;
            }

            m_CurrentDurationPanel = m_DurationPanels[(int)duration.Type];

            if (m_CurrentDurationPanel != null)
            {
                m_CurrentDurationPanel.Visible = true;
            }
        }

        private void UpdateTargetPanelVisibility(ITarget target)
        {
            ZoneTargetPanel.Visible = (target.Type != ITarget.TargetType.Null);
            TargetDirtyPictureBox.Visible = target.IsDirty;

            switch (target.Type)
            {
                case ITarget.TargetType.Null:
                    {
                        break;
                    }
                case ITarget.TargetType.Speed:
                    {
                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)target;

                        RangeTargetUnitsLabel.Visible = true;
                        HRRangeReferenceComboBox.Visible = false;

                        BuildSpeedComboBox(baseTarget);
                        switch (baseTarget.ConcreteTarget.Type)
                        {
                            case IConcreteSpeedTarget.SpeedTargetType.ZoneGTC:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    break;
                                }
                            case IConcreteSpeedTarget.SpeedTargetType.ZoneST:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    break;
                                }
                            case IConcreteSpeedTarget.SpeedTargetType.Range:
                                {
                                    LowRangeTargetLabel.Visible = true;
                                    LowRangeTargetText.Visible = true;
                                    MiddleRangeTargetLabel.Visible = true;
                                    HighRangeTargetText.Visible = true;
                                    RangeTargetUnitsLabel.Visible = true;
                                    break;
                                }
                        }
                        break;
                    }
                case ITarget.TargetType.Cadence:
                    {
                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)target;

                        RangeTargetUnitsLabel.Visible = true;
                        HRRangeReferenceComboBox.Visible = false;

                        BuildCadenceComboBox(baseTarget);
                        switch (baseTarget.ConcreteTarget.Type)
                        {
                            case IConcreteCadenceTarget.CadenceTargetType.ZoneST:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    break;
                                }
                            case IConcreteCadenceTarget.CadenceTargetType.Range:
                                {
                                    LowRangeTargetLabel.Visible = true;
                                    LowRangeTargetText.Visible = true;
                                    MiddleRangeTargetLabel.Visible = true;
                                    HighRangeTargetText.Visible = true;
                                    RangeTargetUnitsLabel.Visible = true;
                                    break;
                                }
                        }
                        break;
                    }
                case ITarget.TargetType.HeartRate:
                    {
                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)target;

                        RangeTargetUnitsLabel.Visible = false;
                        HRRangeReferenceComboBox.Visible = true;

                        BuildHRComboBox(baseTarget);
                        switch (baseTarget.ConcreteTarget.Type)
                        {
                            case IConcreteHeartRateTarget.HeartRateTargetType.ZoneGTC:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    HRRangeReferenceComboBox.Visible = false;
                                    break;
                                }
                            case IConcreteHeartRateTarget.HeartRateTargetType.ZoneST:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    HRRangeReferenceComboBox.Visible = false;
                                    break;
                                }
                            case IConcreteHeartRateTarget.HeartRateTargetType.Range:
                                {
                                    LowRangeTargetLabel.Visible = true;
                                    LowRangeTargetText.Visible = true;
                                    MiddleRangeTargetLabel.Visible = true;
                                    HighRangeTargetText.Visible = true;
                                    RangeTargetUnitsLabel.Visible = false;
                                    HRRangeReferenceComboBox.Visible = true;
                                    break;
                                }
                        }
                        break;
                    }
                case ITarget.TargetType.Power:
                    {
                        BasePowerTarget baseTarget = (BasePowerTarget)target;

                        RangeTargetUnitsLabel.Visible = true;
                        HRRangeReferenceComboBox.Visible = false;

                        BuildPowerComboBox(baseTarget);
                        switch (baseTarget.ConcreteTarget.Type)
                        {
                            case IConcretePowerTarget.PowerTargetType.ZoneGTC:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    break;
                                }
                            case IConcretePowerTarget.PowerTargetType.ZoneST:
                                {
                                    LowRangeTargetLabel.Visible = false;
                                    LowRangeTargetText.Visible = false;
                                    MiddleRangeTargetLabel.Visible = false;
                                    HighRangeTargetText.Visible = false;
                                    RangeTargetUnitsLabel.Visible = false;
                                    break;
                                }
                            case IConcretePowerTarget.PowerTargetType.Range:
                                {
                                    LowRangeTargetLabel.Visible = true;
                                    LowRangeTargetText.Visible = true;
                                    MiddleRangeTargetLabel.Visible = true;
                                    HighRangeTargetText.Visible = true;
                                    RangeTargetUnitsLabel.Visible = true;
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Trace.Assert(false);
                        break;
                    }
            }
        }

        private void UpdateUIFromWorkout(List<Workout> workouts)
        {
            if (workouts.Count <= 1)
            {
                UpdateUIFromWorkout(SelectedWorkout, SelectedStep);
            }
            else
            {
                bool hasScheduledDate = false;
                bool areAllWorkoutsScheduledOnDate = true;

                if (m_SelectedCategory == null)
                {
                    NewWorkoutButton.Enabled = false;
                }
                else
                {
                    NewWorkoutButton.Enabled = true;
                }

                StepSplit.Panel1.Enabled = false;
                RemoveWorkoutButton.Enabled = true;
                StepSplit.Panel2.Enabled = false;
                AddStepButton.Enabled = false;
                AddRepeatButton.Enabled = false;
                RemoveItemButton.Enabled = false;
                WorkoutCalendar.Enabled = true;
                ExportDateTextLabel.Enabled = false;
                ExportDateLabel.Enabled = false;

                if (StepsList.RowData != null)
                {
                    ((List<TreeList.TreeListNode>)StepsList.RowData).Clear();
                    StepsList.RowData = StepsList.RowData;
                }

                // Highlight scheduled dates
                WorkoutCalendar.RemoveAllMarkedDatesStyle(ZoneFiveSoftware.Common.Visuals.Calendar.MarkerStyle.RedTriangle);
                for (int i = 0; i < workouts.Count; ++i)
                {
                    bool foundSelectedDatePlanned = false;
                    Workout currentWorkout = workouts[i];

                    for (int j = 0; j < currentWorkout.ScheduledDates.Count; ++j)
                    {
                        if (WorkoutCalendar.SelectedDate == currentWorkout.ScheduledDates[j])
                        {
                            foundSelectedDatePlanned = true;
                            hasScheduledDate = true;
                        }

                        WorkoutCalendar.AddMarkedDateStyle(currentWorkout.ScheduledDates[j], ZoneFiveSoftware.Common.Visuals.Calendar.MarkerStyle.RedTriangle);
                    }

                    if (!foundSelectedDatePlanned)
                    {
                        areAllWorkoutsScheduledOnDate = false;
                    }
                }

                ScheduleWorkoutButton.Enabled = WorkoutCalendar.SelectedDate >= DateTime.Today && !areAllWorkoutsScheduledOnDate;
                RemoveScheduledDateButton.Enabled = hasScheduledDate;
            }
        }

        private void UpdateUIFromWorkout(Workout workout)
        {
            UpdateUIFromWorkout(workout, new List<IStep>(0));
        }

        private void UpdateUIFromWorkout(Workout workout, IStep forcedSelection)
        {
            List<IStep> selection = new List<IStep>(1);

            selection.Add(forcedSelection);
            UpdateUIFromWorkout(workout, selection);
        }

        private void UpdateUIFromWorkout(Workout workout, List<IStep> forcedSelection)
        {
            PaintEnabled = false;

            if (m_SelectedCategory == null)
            {
                NewWorkoutButton.Enabled = false;
            }
            else
            {
                NewWorkoutButton.Enabled = true;
            }

            if (workout == null)
            {
                StepSplit.Panel1.Enabled = false;
                RemoveWorkoutButton.Enabled = false;
                StepSplit.Panel2.Enabled = false;
                AddStepButton.Enabled = false;
                AddRepeatButton.Enabled = false;
                RemoveItemButton.Enabled = false;
                WorkoutCalendar.Enabled = false;
                ScheduleWorkoutButton.Enabled = false;
                RemoveScheduledDateButton.Enabled = false;
                ExportDateTextLabel.Enabled = false;
                ExportDateLabel.Enabled = false;

                if (StepsList.RowData != null)
                {
                    ((List<TreeList.TreeListNode>)StepsList.RowData).Clear();
                    StepsList.RowData = StepsList.RowData;
                }

                WorkoutCalendar.RemoveAllMarkedDatesStyle(ZoneFiveSoftware.Common.Visuals.Calendar.MarkerStyle.RedTriangle);
            }
            else
            {
                RemoveWorkoutButton.Enabled = true;
                ScheduleWorkoutButton.Enabled = true;
                StepSplit.Panel1.Enabled = true;
                AddStepButton.Enabled = workout.GetStepCount() < 20;
                AddRepeatButton.Enabled = workout.GetStepCount() < 19;
                RemoveItemButton.Enabled = true;
                WorkoutCalendar.Enabled = true;
                ExportDateTextLabel.Enabled = true;
                ExportDateLabel.Enabled = true;

                if (workout.LastExportDate.Ticks == 0)
                {
                    ExportDateLabel.Text = m_ResourceManager.GetString("NeverExportedText", m_CurrentCulture);
                }
                else
                {
                    ExportDateLabel.Text = workout.LastExportDate.ToString(CultureInfo.CreateSpecificCulture(m_CurrentCulture.Name).DateTimeFormat.ShortDatePattern) + " " + workout.LastExportDate.ToString(CultureInfo.CreateSpecificCulture(m_CurrentCulture.Name).DateTimeFormat.ShortTimePattern);
                }

                ScheduleWorkoutButton.Enabled = WorkoutCalendar.SelectedDate >= DateTime.Today && !workout.ScheduledDates.Contains(WorkoutCalendar.SelectedDate);
                RemoveScheduledDateButton.Enabled = workout.ScheduledDates.Contains(WorkoutCalendar.SelectedDate);

                // Update control with workout data
                WorkoutNameText.Text = workout.Name;
                WorkoutNotesText.Text = workout.Notes;

                if(StepsList.RowData == null)
                {
                    StepsList.RowData = new List<TreeList.TreeListNode>();
                }
                else
                {
                    ((List<TreeList.TreeListNode>)StepsList.RowData).Clear();
                }

                AddStepsToList((List<TreeList.TreeListNode>)StepsList.RowData, workout.Steps, null);
                StepsList.Columns.Clear();
                StepsList.Columns.Add(new TreeList.Column("DisplayString", "Description", 350,
                                                          StringAlignment.Near));

                // I don't know why but Invalidate() doesn't refresh the display resulting in
                //  an empty list.  So we force it by reassigning RowData with it's own value
                StepsList.RowData = StepsList.RowData;

                // Highlight scheduled dates
                WorkoutCalendar.RemoveAllMarkedDatesStyle(ZoneFiveSoftware.Common.Visuals.Calendar.MarkerStyle.RedTriangle);
                for(int i = 0 ;i < workout.ScheduledDates.Count; ++i)
                {
                    WorkoutCalendar.AddMarkedDateStyle(workout.ScheduledDates[i], ZoneFiveSoftware.Common.Visuals.Calendar.MarkerStyle.RedTriangle);
                }

                // Force selection
                if (forcedSelection != null)
                {
                    List<StepWrapper> newSelection = new List<StepWrapper>();

                    for (int i = 0; i < forcedSelection.Count; ++i)
                    {
                        StepWrapper wrapper = GetStepWrapper((List<TreeList.TreeListNode>)StepsList.RowData, forcedSelection[i]);

                        if (wrapper != null)
                        {
                            newSelection.Add(wrapper);
                        }
                    }

                    // Force update
                    StepsList.Selected = newSelection;
                }

                UpdateUIFromStep(SelectedSteps);
            }

            PaintEnabled = true;
        }

        private void UpdateUIFromStep(List<IStep> steps)
        {
            if (steps.Count <= 1)
            {
                AddStepButton.Enabled = SelectedWorkout.GetStepCount() < 20;
                AddRepeatButton.Enabled = SelectedWorkout.GetStepCount() < 19;
                StepsNotesSplitter.Enabled = true;

                UpdateUIFromStep(SelectedStep);
            }
            else
            {
                AddStepButton.Enabled = false;
                AddRepeatButton.Enabled = false;
                MoveDownButton.Enabled = false;
                MoveUpButton.Enabled = false;
                StepsNotesSplitter.Enabled = false;
            }
        }

        private void UpdateUIFromStep(IStep step)
        {
            PaintEnabled = false;

            if (step == null)
            {
                StepSplit.Panel2.Enabled = false;
                MoveUpButton.Enabled = false;
                MoveDownButton.Enabled = false;
                RemoveItemButton.Enabled = false;
            }
            else
            {
                UInt16 selectedPosition = 0;
                List<IStep> selectedList = null;

                StepSplit.Panel2.Enabled = true;
                RemoveItemButton.Enabled = true;
                Utils.GetStepInfo(SelectedStep, SelectedWorkout.Steps, out selectedList, out selectedPosition);
                MoveUpButton.Enabled = selectedPosition != 0; // Not the first step
                MoveDownButton.Enabled = selectedPosition < selectedList.Count - 1; // Not the last step

                StepNotesText.Text = step.Notes;

                switch (step.Type)
                {
                    case IStep.StepType.Regular:
                        {
                            // Show correct panels/controls
                            RepetitionPropertiesGroup.Visible = false;
                            StepDurationGroup.Visible = true;
                            StepTargetGroup.Visible = true;
                            StepNameLabel.Visible = true;
                            StepNameText.Visible = true;
                            RestingCheckBox.Visible = true;

                            RegularStep concreteStep = (RegularStep)step;

                            if (concreteStep.Name != null && concreteStep.Name != String.Empty)
                            {
                                StepNameText.Text = concreteStep.Name;
                            }
                            else
                            {
                                StepNameText.Text = "";
                            }
                            RestingCheckBox.Checked = concreteStep.IsRestingStep;
                            DurationComboBox.SelectedIndex = (int)concreteStep.Duration.Type;
                            TargetComboBox.SelectedIndex = (int)concreteStep.Target.Type;

                            // Update correct duration UI elements based on type
                            UpdateDurationPanelVisibility(concreteStep.Duration);
                            switch (concreteStep.Duration.Type)
                            {
                                case IDuration.DurationType.LapButton:
                                    {
                                        break;
                                    }
                                case IDuration.DurationType.Distance:
                                    {
                                        DistanceDuration concreteDuration = (DistanceDuration)concreteStep.Duration;
                                        double distance = concreteDuration.GetDistanceInUnits(SelectedWorkout.Category.DistanceUnits);
                                        DistanceDurationText.Text = String.Format("{0:0.00}", distance);
                                        DistanceDurationUnitsLabel.Text = Length.LabelAbbr(SelectedWorkout.Category.DistanceUnits);
                                        break;
                                    }
                                case IDuration.DurationType.Time:
                                    {
                                        TimeDuration concreteDuration = (TimeDuration)concreteStep.Duration;
                                        TimeDurationUpDown.Duration = concreteDuration.TimeInSeconds;
                                        break;
                                    }
                                case IDuration.DurationType.HeartRateAbove:
                                    {
                                        HeartRateAboveDuration concreteDuration = (HeartRateAboveDuration)concreteStep.Duration;
                                        HeartRateDurationText.Text = concreteDuration.MaxHeartRate.ToString();
                                        if (concreteDuration.IsPercentageMaxHeartRate)
                                        {
                                            HeartRateReferenceComboBox.SelectedIndex = 1;
                                        }
                                        else
                                        {
                                            HeartRateReferenceComboBox.SelectedIndex = 0;
                                        }
                                        break;
                                    }
                                case IDuration.DurationType.HeartRateBelow:
                                    {
                                        HeartRateBelowDuration concreteDuration = (HeartRateBelowDuration)concreteStep.Duration;
                                        HeartRateDurationText.Text = concreteDuration.MinHeartRate.ToString();
                                        if (concreteDuration.IsPercentageMaxHeartRate)
                                        {
                                            HeartRateReferenceComboBox.SelectedIndex = 1;
                                        }
                                        else
                                        {
                                            HeartRateReferenceComboBox.SelectedIndex = 0;
                                        }
                                        break;
                                    }
                                case IDuration.DurationType.Calories:
                                    {
                                        CaloriesDuration concreteDuration = (CaloriesDuration)concreteStep.Duration;
                                        CaloriesDurationText.Text = concreteDuration.CaloriesToSpend.ToString();
                                        break;
                                    }
                                default:
                                    {
                                        Trace.Assert(false);
                                        break;
                                    }
                            }

                            UpdateTargetPanelVisibility(concreteStep.Target);
                            switch(concreteStep.Target.Type)
                            {
                                case ITarget.TargetType.Null:
                                    {
                                        break;
                                    }
                                case ITarget.TargetType.Speed:
                                    {
                                        BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;

                                        BuildSpeedComboBox(baseTarget);
                                        switch (baseTarget.ConcreteTarget.Type)
                                        {
                                            case IConcreteSpeedTarget.SpeedTargetType.ZoneGTC:
                                                {
                                                    SpeedZoneGTCTarget concreteTarget = (SpeedZoneGTCTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = concreteTarget.Zone;
                                                    break;
                                                }
                                            case IConcreteSpeedTarget.SpeedTargetType.ZoneST:
                                                {
                                                    SpeedZoneSTTarget concreteTarget = (SpeedZoneSTTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = Utils.FindIndexForZone(baseTarget.ParentStep.ParentWorkout.Category.SpeedZone.Zones,
                                                                                                        concreteTarget.Zone) + 1;
                                                    break;
                                                }
                                            case IConcreteSpeedTarget.SpeedTargetType.Range:
                                                {
                                                    Length.Units unit = SelectedWorkout.Category.DistanceUnits;
                                                    SpeedRangeTarget concreteTarget = (SpeedRangeTarget)baseTarget.ConcreteTarget;

                                                    if (concreteTarget.ViewAsPace)
                                                    {
                                                        RangeTargetUnitsLabel.Text = m_ResourceManager.GetString("MinuteAbbrText", m_CurrentCulture) + "/" + Length.LabelAbbr(unit);
                                                        double min = concreteTarget.GetMinSpeedInMinutesPerUnit(unit);
                                                        double max = concreteTarget.GetMaxSpeedInMinutesPerUnit(unit);
                                                        UInt16 minutes;
                                                        UInt16 seconds;

                                                        Utils.FloatToTime(min, out minutes, out seconds);
                                                        HighRangeTargetText.Text = String.Format("{0:00}:{1:00}", minutes, seconds);
                                                        Utils.FloatToTime(max, out minutes, out seconds);
                                                        LowRangeTargetText.Text = String.Format("{0:00}:{1:00}", minutes, seconds);
                                                    }
                                                    else
                                                    {
                                                        RangeTargetUnitsLabel.Text = Length.LabelAbbr(unit) + m_ResourceManager.GetString("PerHourText", m_CurrentCulture);
                                                        LowRangeTargetText.Text = String.Format("{0:0.00}", concreteTarget.GetMinSpeedInUnitsPerHour(unit));
                                                        HighRangeTargetText.Text = String.Format("{0:0.00}", concreteTarget.GetMaxSpeedInUnitsPerHour(unit));
                                                    }
                                                    
                                                    ZoneComboBox.SelectedIndex = 0;

                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ITarget.TargetType.Cadence:
                                    {
                                        BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;

                                        BuildCadenceComboBox(baseTarget);
                                        switch (baseTarget.ConcreteTarget.Type)
                                        {
                                            case IConcreteCadenceTarget.CadenceTargetType.ZoneST:
                                                {
                                                    CadenceZoneSTTarget concreteTarget = (CadenceZoneSTTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = Utils.FindIndexForZone(Options.CadenceZoneCategory.Zones,
                                                                                                                       concreteTarget.Zone) + 1;
                                                    break;
                                                }
                                            case IConcreteCadenceTarget.CadenceTargetType.Range:
                                                {
                                                    CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;

                                                    RangeTargetUnitsLabel.Text = CommonResources.Text.LabelRPM;
                                                    ZoneComboBox.SelectedIndex = 0;
                                                    LowRangeTargetText.Text = concreteTarget.MinCadence.ToString();
                                                    HighRangeTargetText.Text = concreteTarget.MaxCadence.ToString();
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ITarget.TargetType.HeartRate:
                                    {
                                        BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;

                                        BuildHRComboBox(baseTarget);
                                        switch (baseTarget.ConcreteTarget.Type)
                                        {
                                            case IConcreteHeartRateTarget.HeartRateTargetType.ZoneGTC:
                                                {
                                                    HeartRateZoneGTCTarget concreteTarget = (HeartRateZoneGTCTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = concreteTarget.Zone;
                                                    break;
                                                }
                                            case IConcreteHeartRateTarget.HeartRateTargetType.ZoneST:
                                                {
                                                    HeartRateZoneSTTarget concreteTarget = (HeartRateZoneSTTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = Utils.FindIndexForZone(baseTarget.ParentStep.ParentWorkout.Category.HeartRateZone.Zones,
                                                                                                                       concreteTarget.Zone) + 1;
                                                    break;
                                                }
                                            case IConcreteHeartRateTarget.HeartRateTargetType.Range:
                                                {
                                                    HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = 0;
                                                    LowRangeTargetText.Text = concreteTarget.MinHeartRate.ToString();
                                                    HighRangeTargetText.Text = concreteTarget.MaxHeartRate.ToString();
                                                    HRRangeReferenceComboBox.SelectedIndex = concreteTarget.IsPercentageMaxHeartRate ? 1 : 0;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ITarget.TargetType.Power:
                                    {
                                        HRRangeReferenceComboBox.Visible = false;

                                        BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;

                                        BuildPowerComboBox(baseTarget);
                                        switch (baseTarget.ConcreteTarget.Type)
                                        {
                                            case IConcretePowerTarget.PowerTargetType.ZoneGTC:
                                                {
                                                    PowerZoneGTCTarget concreteTarget = (PowerZoneGTCTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = concreteTarget.Zone;
                                                    break;
                                                }
                                            case IConcretePowerTarget.PowerTargetType.ZoneST:
                                                {
                                                    PowerZoneSTTarget concreteTarget = (PowerZoneSTTarget)baseTarget.ConcreteTarget;
                                                    ZoneComboBox.SelectedIndex = Utils.FindIndexForZone(Options.PowerZoneCategory.Zones,
                                                                                                        concreteTarget.Zone) + 1;
                                                    break;
                                                }
                                            case IConcretePowerTarget.PowerTargetType.Range:
                                                {
                                                    PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;

                                                    RangeTargetUnitsLabel.Text = CommonResources.Text.LabelWatts;
                                                    ZoneComboBox.SelectedIndex = 0;
                                                    LowRangeTargetText.Text = concreteTarget.MinPower.ToString();
                                                    HighRangeTargetText.Text = concreteTarget.MaxPower.ToString();
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        Trace.Assert(false);
                                        break;
                                    }
                            }

                            break;
                        }
                    case IStep.StepType.Repeat:
                        {
                            // Show correct panels/controls
                            RepetitionPropertiesGroup.Visible = true;
                            StepDurationGroup.Visible = false;
                            StepTargetGroup.Visible = false;
                            StepNameLabel.Visible = false;
                            StepNameText.Visible = false;
                            RestingCheckBox.Visible = false;

                            RepeatStep concreteStep = (RepeatStep)step;

                            RepetitionCountText.Text = concreteStep.RepetitionCount.ToString();

                            break;
                        }
                    default:
                        {
                            Trace.Assert(false);
                            break;
                        }
                }
            }

            PaintEnabled = true;
        }

        private void BuildCadenceComboBox(BaseCadenceTarget target)
        {
            ZoneComboBox.Items.Clear();
            ZoneComboBox.Items.Add(m_ResourceManager.GetString("CustomText"));

            IList<INamedLowHighZone> zones = Options.CadenceZoneCategory.Zones;
            for (byte i = 0; i < zones.Count; ++i)
            {
                ZoneComboBox.Items.Add(zones[i].Name);
            }
        }

        private void BuildHRComboBox(BaseHeartRateTarget target)
        {
            IConcreteHeartRateTarget.HeartRateTargetType type = target.ConcreteTarget.Type;

            ZoneComboBox.Items.Clear();
            ZoneComboBox.Items.Add(m_ResourceManager.GetString("CustomText"));

            if(type == IConcreteHeartRateTarget.HeartRateTargetType.ZoneGTC ||
               (type == IConcreteHeartRateTarget.HeartRateTargetType.Range && !Options.UseSportTracksHeartRateZones))
            {
                for (byte i = 1; i <= 5; ++i)
                {
                    ZoneComboBox.Items.Add(i.ToString());
                }
            }
            // Use ST zones
            else
            {
                IList<INamedLowHighZone> zones = target.ParentStep.ParentWorkout.Category.HeartRateZone.Zones;
                for (byte i = 0; i < zones.Count; ++i)
                {
                    ZoneComboBox.Items.Add(zones[i].Name);
                }
            }
        }

        private void BuildPowerComboBox(BasePowerTarget target)
        {
            IConcretePowerTarget.PowerTargetType type = target.ConcreteTarget.Type;

            ZoneComboBox.Items.Clear();
            ZoneComboBox.Items.Add(m_ResourceManager.GetString("CustomText"));

            if (type == IConcretePowerTarget.PowerTargetType.ZoneGTC ||
               (type == IConcretePowerTarget.PowerTargetType.Range && !Options.UseSportTracksPowerZones))
            {
                for (byte i = 1; i <= 7; ++i)
                {
                    ZoneComboBox.Items.Add(i.ToString());
                }
            }
            // Use ST zones
            else
            {
                IList<INamedLowHighZone> zones = Options.PowerZoneCategory.Zones;
                for (byte i = 0; i < zones.Count; ++i)
                {
                    ZoneComboBox.Items.Add(zones[i].Name);
                }
            }
        }

        private void BuildSpeedComboBox(BaseSpeedTarget target)
        {
            IConcreteSpeedTarget.SpeedTargetType type = target.ConcreteTarget.Type;

            ZoneComboBox.Items.Clear();
            ZoneComboBox.Items.Add(m_ResourceManager.GetString("CustomText"));

            // Use GTC zones
            if (type == IConcreteSpeedTarget.SpeedTargetType.ZoneGTC ||
               (type == IConcreteSpeedTarget.SpeedTargetType.Range && !Options.UseSportTracksHeartRateZones))
            {
                for (byte i = 1; i <= 10; ++i)
                {
                    ZoneComboBox.Items.Add(m_ResourceManager.GetString("GTCSpeedZone" + i.ToString() + "Text", m_CurrentCulture));
                }
            }
            // Use ST zones
            else
            {
                IList<INamedLowHighZone> zones = target.ParentStep.ParentWorkout.Category.SpeedZone.Zones;
                for (byte i = 0; i < zones.Count; ++i)
                {
                    ZoneComboBox.Items.Add(zones[i].Name);
                }
            }
        }

        private void AddStepsToList(List<TreeList.TreeListNode> list, List<IStep> steps, StepWrapper parent)
        {
            for (int i = 0; i < steps.Count; ++i)
            {
                IStep currentStep = steps[i];
                StepWrapper newStep = new StepWrapper(parent, currentStep);

                if (parent != null)
                {
                    parent.Children.Add(newStep);
                }
                else
                {
                    list.Add(newStep);
                }

                if (steps[i].Type == IStep.StepType.Repeat)
                {
                    RepeatStep concreteStep = (RepeatStep)currentStep;

                    AddStepsToList(list, concreteStep.StepsToRepeat, newStep);
                }
            }
        }

        private void RefreshCalendarView()
        {
            List<DateTime> highlights = new List<DateTime>();

            for(int i = 0; i < WorkoutManager.Instance.Workouts.Count; ++i)
            {
                Workout currentWorkout = WorkoutManager.Instance.Workouts[i];

                for (int j = 0; j < currentWorkout.ScheduledDates.Count; ++j)
                {
                    highlights.Add(currentWorkout.ScheduledDates[j]);
                }
            }

            PluginMain.GetApplication().Calendar.SetHighlightedDates(highlights);
            PluginMain.GetApplication().Calendar.Selected = PluginMain.GetApplication().Calendar.Selected;
        }

        private void AddNewStep(IStep newStep)
        {
            Trace.Assert(SelectedWorkout != null);
            UInt16 selectedPosition = 0;
            List<IStep> selectedList = null;
            bool selectionFound;

            selectionFound = Utils.GetStepInfo(SelectedStep, SelectedWorkout.Steps, out selectedList, out selectedPosition);
            SelectedStep = newStep;

            if (selectionFound)
            {
                // Insert after selected
                selectedList.Insert(selectedPosition + 1, SelectedStep);
            }
            else
            {
                // Insert as 1st element
                SelectedWorkout.Steps.Insert(0, SelectedStep);
            }

            Trace.Assert(StepsList.Selected != null);
            StepsList.Selected.Clear();
            StepsList.Selected.Add(SelectedStep);

            UpdateUIFromWorkout(SelectedWorkout, SelectedStep);
        }

        private void AddNewWorkout()
        {
            List<TreeList.TreeListNode> selection = new List<TreeList.TreeListNode>();
            WorkoutWrapper wrapper;
            Trace.Assert(m_SelectedCategory != null);

            SelectedWorkout = WorkoutManager.Instance.CreateWorkout(m_SelectedCategory);
            wrapper = AddWorkoutToList((List<TreeList.TreeListNode>)WorkoutsList.RowData, SelectedWorkout);
            selection.Add(wrapper);
            WorkoutsList.Selected = selection;

            // Force list update
            WorkoutsList.RowData = WorkoutsList.RowData;

            UpdateUIFromWorkout(SelectedWorkout);
        }

        private StepWrapper GetStepWrapper(List<TreeList.TreeListNode> list, IStep step)
        {
            for (UInt16 i = 0; i < list.Count; ++i)
            {
                StepWrapper currentStep = (StepWrapper)list[i];

                if (currentStep.Element == step)
                {
                    return currentStep;
                }
                else if (((IStep)currentStep.Element).Type == IStep.StepType.Repeat)
                {
                    StepWrapper childWrapper = GetStepWrapper((List<TreeList.TreeListNode>)currentStep.Children, step);

                    if (childWrapper != null)
                    {
                        return childWrapper;
                    }
                }
            }

            return null;
        }

        private WorkoutWrapper GetWorkoutWrapper(List<TreeList.TreeListNode> list, Workout workout)
        {
            for (UInt16 i = 0; i < list.Count; ++i)
            {
                if (list[i].GetType() == typeof(WorkoutWrapper))
                {
                    WorkoutWrapper currentWorkout = (WorkoutWrapper)list[i];

                    if (currentWorkout.Element == workout)
                    {
                        return currentWorkout;
                    }
                }
                else
                {
                    Trace.Assert(list[i].GetType() == typeof(ActivityCategoryWrapper));

                    ActivityCategoryWrapper currentCategory = (ActivityCategoryWrapper)list[i];
                    WorkoutWrapper childWrapper = GetWorkoutWrapper((List<TreeList.TreeListNode>)currentCategory.Children, workout);

                    if (childWrapper != null)
                    {
                        return childWrapper;
                    }

                }
            }

            return null;
        }

        private void CleanUpWorkoutAfterDelete(Workout workout)
        {
            if (workout.Steps.Count > 0)
            {
                // Go through repeat steps and delete the ones which have 0 substeps
                for (int i = 0; i < workout.Steps.Count; ++i)
                {
                    IStep currentStep = workout.Steps[i];

                    if (currentStep.Type == IStep.StepType.Repeat)
                    {
                        RepeatStep concreteStep = (RepeatStep)currentStep;

                        if (concreteStep.StepsToRepeat.Count > 0)
                        {
                            CleanUpWorkoutAfterDelete(concreteStep);
                        }

                        if(concreteStep.StepsToRepeat.Count == 0)
                        {
                            workout.Steps.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            if (workout.Steps.Count == 0)
            {
                // Cannot have an empty workout, recreate a base step
                workout.Steps.Add(new RegularStep(SelectedWorkout));
            }
        }

        private void CleanUpWorkoutAfterDelete(RepeatStep step)
        {
            // Go through repeat steps and delete the ones which have 0 substeps
            for (int i = 0; i < step.StepsToRepeat.Count; ++i)
            {
                IStep currentStep = step.StepsToRepeat[i];

                if (currentStep.Type == IStep.StepType.Repeat)
                {
                    RepeatStep concreteStep = (RepeatStep)currentStep;

                    if (concreteStep.StepsToRepeat.Count == 0)
                    {
                        step.StepsToRepeat.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        CleanUpWorkoutAfterDelete(concreteStep);
                    }
                }
            }
        }

        public void BuildWorkoutsList()
        {
            IApplication app = PluginMain.GetApplication();
            List<TreeList.TreeListNode> categories = new List<TreeList.TreeListNode>();
            List<TreeList.TreeListNode> selection = new List<TreeList.TreeListNode>();

            for (int i = 0; i < app.Logbook.ActivityCategories.Count; ++i)
            {
                IActivityCategory currentCategory = app.Logbook.ActivityCategories[i];
                ActivityCategoryWrapper newNode = new ActivityCategoryWrapper(null, currentCategory);

                categories.Add(newNode);
                AddCategoryNode(newNode, null);

                if (m_SelectedWorkouts.Count == 0 && i == 0)
                {
                    selection.Add(newNode);
                }
            }

            for (int i = 0; i < WorkoutManager.Instance.Workouts.Count; ++i)
            {
                WorkoutWrapper newItem = AddWorkoutToList(categories, WorkoutManager.Instance.Workouts[i]);

                if (m_SelectedWorkouts.Contains(WorkoutManager.Instance.Workouts[i]))
                {
                    selection.Add(newItem);
                }
            }

            if (selection.Count == 0)
            {
                selection.Add(categories[0]);
            }

            WorkoutsList.RowData = categories;
            WorkoutsList.Columns.Clear();
            WorkoutsList.Columns.Add(new TreeList.Column("Name", m_ResourceManager.GetString("CategoryText", m_CurrentCulture),
                                                         150, StringAlignment.Near));
            WorkoutsList.Selected = selection;
            WorkoutsList.SetExpanded(WorkoutsList.RowData, true, true);
        }

        private WorkoutWrapper AddWorkoutToList(List<TreeList.TreeListNode> list, Workout workout)
        {
            // Go throough category list
            for(int i = 0; i < list.Count; ++i)
            {
                if(list[i].GetType() == typeof(ActivityCategoryWrapper))
                {
                    ActivityCategoryWrapper currentCategory = (ActivityCategoryWrapper)list[i];

                    if (currentCategory.Element == workout.Category)
                    {
                        WorkoutWrapper wrapper = new WorkoutWrapper(currentCategory, workout);

                        int index = 0;
                        while(index < currentCategory.Children.Count &&
                              (currentCategory.Children[index].GetType() != typeof(WorkoutWrapper) ||
                              ((Workout)((WorkoutWrapper)currentCategory.Children[index]).Element).Name.CompareTo(wrapper.Name) < 0))
                        {
                            index++;
                        }

                        currentCategory.Children.Insert(index, wrapper);

                        return wrapper;
                    }
                    else if(currentCategory.Children.Count > 0)
                    {
                        WorkoutWrapper result;

                        result = AddWorkoutToList((List<TreeList.TreeListNode>)currentCategory.Children, workout);

                        if(result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        private void UpdateHeartRateTargetFromComboBox(RegularStep concreteStep, int selectedIndex)
        {
            BaseHeartRateTarget baseTarget = (BaseHeartRateTarget)concreteStep.Target;

            // We might have to change from one target type to the other
            if (baseTarget.ConcreteTarget.Type != IConcreteHeartRateTarget.HeartRateTargetType.Range &&
                selectedIndex == 0)
            {
                // Custom range
                baseTarget.ConcreteTarget = new HeartRateRangeTarget(baseTarget);
                UpdateUIFromStep(SelectedStep);
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range &&
                    selectedIndex != 0)
            {
                if (Options.UseSportTracksHeartRateZones)
                {
                    baseTarget.ConcreteTarget = new HeartRateZoneSTTarget(baseTarget);
                }
                else
                {
                    baseTarget.ConcreteTarget = new HeartRateZoneGTCTarget(baseTarget);
                }
                UpdateUIFromStep(SelectedStep);
            }

            if (baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.Range)
            {
                HeartRateRangeTarget concreteTarget = (HeartRateRangeTarget)baseTarget.ConcreteTarget;

                LowRangeTargetText.Text = concreteTarget.MinHeartRate.ToString();
                HighRangeTargetText.Text = concreteTarget.MaxHeartRate.ToString();
                HRRangeReferenceComboBox.SelectedIndex = concreteTarget.IsPercentageMaxHeartRate ? 1 : 0;
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.ZoneST)
            {
                HeartRateZoneSTTarget concreteTarget = (HeartRateZoneSTTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = baseTarget.ParentStep.ParentWorkout.Category.HeartRateZone.Zones[selectedIndex - 1];
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteHeartRateTarget.HeartRateTargetType.ZoneGTC)
            {
                HeartRateZoneGTCTarget concreteTarget = (HeartRateZoneGTCTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = (Byte)(selectedIndex);
            }
        }

        private void UpdateSpeedTargetFromComboBox(RegularStep concreteStep, int selectedIndex)
        {
            BaseSpeedTarget baseTarget = (BaseSpeedTarget)concreteStep.Target;

            // We might have to change from one target type to the other
            if (baseTarget.ConcreteTarget.Type != IConcreteSpeedTarget.SpeedTargetType.Range &&
                selectedIndex == 0)
            {
                // Custom range
                baseTarget.ConcreteTarget = new SpeedRangeTarget(baseTarget);
                UpdateUIFromStep(SelectedStep);
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range &&
                    selectedIndex != 0)
            {
                if (Options.UseSportTracksHeartRateZones)
                {
                    baseTarget.ConcreteTarget = new SpeedZoneSTTarget(baseTarget);
                }
                else
                {
                    baseTarget.ConcreteTarget = new SpeedZoneGTCTarget(baseTarget);
                }
                UpdateUIFromStep(SelectedStep);
            }

            if (baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.Range)
            {
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.ZoneST)
            {
                SpeedZoneSTTarget concreteTarget = (SpeedZoneSTTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = baseTarget.ParentStep.ParentWorkout.Category.SpeedZone.Zones[selectedIndex - 1];
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteSpeedTarget.SpeedTargetType.ZoneGTC)
            {
                SpeedZoneGTCTarget concreteTarget = (SpeedZoneGTCTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = (Byte)(selectedIndex);
            }
        }

        private void UpdateCadenceTargetFromComboBox(RegularStep concreteStep, int selectedIndex)
        {
            BaseCadenceTarget baseTarget = (BaseCadenceTarget)concreteStep.Target;

            // We might have to change from one target type to the other
            if (baseTarget.ConcreteTarget.Type != IConcreteCadenceTarget.CadenceTargetType.Range &&
                selectedIndex == 0)
            {
                // Custom range
                baseTarget.ConcreteTarget = new CadenceRangeTarget(baseTarget);
                UpdateUIFromStep(SelectedStep);
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range &&
                     selectedIndex != 0)
            {
                // ST zone
                baseTarget.ConcreteTarget = new CadenceZoneSTTarget(baseTarget);
                UpdateUIFromStep(SelectedStep);
            }

            if (baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.Range)
            {
                CadenceRangeTarget concreteTarget = (CadenceRangeTarget)baseTarget.ConcreteTarget;

                LowRangeTargetText.Text = concreteTarget.MinCadence.ToString();
                HighRangeTargetText.Text = concreteTarget.MaxCadence.ToString();
            }
            else if (baseTarget.ConcreteTarget.Type == IConcreteCadenceTarget.CadenceTargetType.ZoneST)
            {
                CadenceZoneSTTarget concreteTarget = (CadenceZoneSTTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = Options.CadenceZoneCategory.Zones[selectedIndex - 1];
            }
        }

        private void UpdatePowerTargetFromComboBox(RegularStep concreteStep, int selectedIndex)
        {
            BasePowerTarget baseTarget = (BasePowerTarget)concreteStep.Target;

            // We might have to change from one target type to the other
            if (baseTarget.ConcreteTarget.Type != IConcretePowerTarget.PowerTargetType.Range &&
                selectedIndex == 0)
            {
                // Custom range
                baseTarget.ConcreteTarget = new PowerRangeTarget(baseTarget);
                UpdateUIFromStep(SelectedStep);
            }
            else if (baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range &&
                     selectedIndex != 0)
            {
                // ST zone
                if (Options.UseSportTracksHeartRateZones)
                {
                    baseTarget.ConcreteTarget = new PowerZoneSTTarget(baseTarget);
                }
                else
                {
                    baseTarget.ConcreteTarget = new PowerZoneGTCTarget(baseTarget);
                }
                UpdateUIFromStep(SelectedStep);
            }

            if (baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.Range)
            {
                PowerRangeTarget concreteTarget = (PowerRangeTarget)baseTarget.ConcreteTarget;

                LowRangeTargetText.Text = concreteTarget.MinPower.ToString();
                HighRangeTargetText.Text = concreteTarget.MaxPower.ToString();
            }
            else if (baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.ZoneST)
            {
                PowerZoneSTTarget concreteTarget = (PowerZoneSTTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = Options.PowerZoneCategory.Zones[selectedIndex - 1];
            }
            else if (baseTarget.ConcreteTarget.Type == IConcretePowerTarget.PowerTargetType.ZoneGTC)
            {
                PowerZoneGTCTarget concreteTarget = (PowerZoneGTCTarget)baseTarget.ConcreteTarget;

                concreteTarget.Zone = (Byte)selectedIndex;
            }
        }

        private void SelectWorkoutsInList(List<Workout> workouts)
        {
            List<TreeList.TreeListNode> selection = new List<TreeList.TreeListNode>();

            for (int i = 0; i < workouts.Count; ++i)
            {
                Workout currentWorkout = workouts[i];

                selection.Add(GetWorkoutWrapper((List<TreeList.TreeListNode>)WorkoutsList.RowData, currentWorkout));
            }

            WorkoutsList.Selected = selection;
        }

        private void SelectStepsInList(List<IStep> steps)
        {
            List<TreeList.TreeListNode> selection = new List<TreeList.TreeListNode>();

            for (int i = 0; i < steps.Count; ++i)
            {
                IStep currentStep = steps[i];

                selection.Add(GetStepWrapper((List<TreeList.TreeListNode>)StepsList.RowData, currentStep));
            }

            StepsList.Selected = selection;
        }

        private void RefreshActions()
        {
            if (PluginMain.GetApplication().ActiveView.GetType() == typeof(GarminWorkoutView))
            {
                for (int i = 0; i < PluginMain.GetApplication().ActiveView.Actions.Count; ++i)
                {
                    PluginMain.GetApplication().ActiveView.Actions[i].Refresh();
                }
            }
        }

        private void DeleteSelectedWorkouts()
        {
            for (int i = 0; i < m_SelectedWorkouts.Count; ++i)
            {
                WorkoutManager.Instance.Workouts.Remove(m_SelectedWorkouts[i]);
            }
            SelectedWorkout = null;

            BuildWorkoutsList();
            UpdateUIFromWorkout(SelectedWorkout);

            Utils.SaveWorkoutsToLogbook();
        }

        private void DeleteSelectedSteps()
        {
            for (int i = 0; i < m_SelectedSteps.Count; ++i)
            {
                DeleteStep(m_SelectedSteps[i]);
            }

            m_SelectedSteps.Clear();
            UpdateUIFromWorkout(SelectedWorkout);
            Utils.SaveWorkoutsToLogbook();
        }

        private void DeleteSelectedStep()
        {
            DeleteStep(SelectedStep);

            UpdateUIFromWorkout(SelectedWorkout);
            Utils.SaveWorkoutsToLogbook();
        }

        private void DeleteStep(IStep step)
        {
            Trace.Assert(SelectedWorkout != null && step != null);
            UInt16 selectedPosition = 0;
            List<IStep> selectedList = null;

            if (Utils.GetStepInfo(step, SelectedWorkout.Steps, out selectedList, out selectedPosition))
            {
                selectedList.RemoveAt(selectedPosition);

                CleanUpWorkoutAfterDelete(SelectedWorkout);
            }
        }

        private List<IStep> GetMinimalStepsBase(List<IStep> steps)
        {
            List<IStep> result = new List<IStep>();
            List<RepeatStep> baseRepeatSteps = new List<RepeatStep>();
            List<RepeatStep> repeatSteps = new List<RepeatStep>();

            // 1st pass, add all the base repeat steps to the result list
            for (int i = 0; i < steps.Count; ++i)
            {
                if (steps[i].Type == IStep.StepType.Repeat)
                {
                    repeatSteps.Add((RepeatStep)steps[i]);
                }
            }

            for (int i = 0; i < repeatSteps.Count; ++i)
            {
                RepeatStep currentRepeat = repeatSteps[i];
                bool isChild = false;

                // We must check if this repeat is a base, or a child of another repeat
                for (int j = 0; j < repeatSteps.Count; j++)
                {
                    if (i != j && repeatSteps[j].IsChildStep(currentRepeat))
                    {
                        isChild = true;
                        break;
                    }
                }

                if(!isChild)
                {
                    baseRepeatSteps.Add(currentRepeat);
                }
            }

            // We now have all base repeat steps in our result, check all regular steps
            //  for inheritance against that base
            for (int i = 0; i < steps.Count; ++i)
            {
                if (steps[i].Type == IStep.StepType.Regular)
                {
                    RegularStep currentStep = (RegularStep)steps[i];
                    bool isChild = false;

                    for (int j = 0; j < baseRepeatSteps.Count; ++j)
                    {
                        // We must check if this repeat is a base, or a child of another repeat
                        if (baseRepeatSteps[j].IsChildStep(currentStep))
                        {
                            isChild = true;
                            break;
                        }
                    }

                    if (!isChild)
                    {
                        result.Add(currentStep);
                    }
                }
                else if(baseRepeatSteps.Contains((RepeatStep)steps[i]))
                {
                    // Add repeats in the right order
                    result.Add(steps[i]);
                }
            }

            return result;
        }

        private int SelectedStepComparison(IStep x, IStep y)
        {
            int xId, yId;

            xId = Utils.GetStepExportId(x);
            yId = Utils.GetStepExportId(y);

            if (xId < yId)
            {
                return -1;
            }
            else if (xId == yId)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        [DesignerSerializationVisibility(0)]
        [Browsable(false)]
        private bool PaintEnabled
        {
            get { return m_PaintDisableCount == 0; }
            set
            {
                StepsList.PaintEnabled = value;
                if (value == true)
                {
                    Trace.Assert(m_PaintDisableCount > 0);
                    m_PaintDisableCount--;

                    if (m_PaintDisableCount == 0)
                    {
                        Invalidate();
                    }
                }
                else
                {
                    m_PaintDisableCount++;
                }
            }
        }

        private Workout SelectedWorkout
        {
            get
            {
                if (m_SelectedWorkouts.Count == 1)
                {
                    return m_SelectedWorkouts[0];
                }

                return null;
            }
            set
            {
                m_SelectedWorkouts.Clear();

                if (value != null)
                {
                    m_SelectedWorkouts.Add(value);
                }
            }
        }

        public List<Workout> SelectedWorkouts
        {
            get { return m_SelectedWorkouts; }
        }

        private IStep SelectedStep
        {
            get
            {
                if (m_SelectedSteps.Count == 1)
                {
                    return m_SelectedSteps[0];
                }

                return null;
            }
            set
            {
                m_SelectedSteps.Clear();

                if (value != null)
                {
                    m_SelectedSteps.Add(value);
                }
            }
        }

        public List<IStep> SelectedSteps
        {
            get { return m_SelectedSteps; }
        }

        public IActivityCategory SelectedCategory
        {
            get
            {
                if (m_SelectedCategory == null)
                {
                    // We probably have multiple workouts selected, find the common category
                    IActivityCategory selection = null;

                    for (int i = 0; i < SelectedWorkouts.Count; ++i)
                    {
                        if (selection == null)
                        {
                            selection = SelectedWorkouts[i].Category;
                        }
                        else if (selection != SelectedWorkouts[i].Category)
                        {
                            return null;
                        }
                    }

                    return selection;
                }

                return m_SelectedCategory;
            }
            set { m_SelectedCategory = value; }
        }

        private enum RangeValidationInputType
        {
            Integer,
            Float,
            Time
        }

        private ResourceManager m_ResourceManager = new ResourceManager("GarminWorkoutPlugin.Resources.StringResources",
                                                                        Assembly.GetExecutingAssembly());
        private CultureInfo m_CurrentCulture;
        private ITheme m_CurrentTheme;
        private ZoneFiveSoftware.Common.Visuals.Panel m_CurrentDurationPanel;
        private readonly ZoneFiveSoftware.Common.Visuals.Panel[] m_DurationPanels;
        private const int CTRL_KEY_CODE = 8;

        private List<Workout> m_SelectedWorkouts = new List<Workout>();
        private List<IStep> m_SelectedSteps = new List<IStep>();
        private IActivityCategory m_SelectedCategory;
        private int m_PaintDisableCount = 0;

        private bool m_IsMouseDownInWorkoutsList = false;
        private bool m_IsMouseDownInStepsList = false;
        private Point m_LastMouseDownLocation;
        private int m_MouseMovedPixels = 0;

        private bool m_CancelledWorkoutSelection = false;
        private bool m_CancelledStepSelection = false;
        private Workout m_SelectedWorkoutCancelled;
        private IStep m_SelectedStepCancelled;
    }
}
