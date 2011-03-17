﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GarminFitnessPlugin.Controller
{
    public class FITConstants
    {
        public static readonly Byte FITProtocolMajorVersion = 1;
        public static readonly Byte FITProtocolMinorVersion = 0;

        public static readonly Byte FITProfileMajorVersion = 1;
        public static readonly Byte FITProfileMinorVersion = 0;
        public static readonly Byte FITProfileMajorVersionMultiplier = 100;

        public static readonly String FITFileDescriptor = ".FIT";
    }

    public enum FITEndianness
    {
        LittleEndian = 0,
        BigEndian,
    }

    public enum FITFileTypes
    {
        Workout = 5,
    }

    public enum FITGlobalMessageIds
    {
        FileId = 0,
        Workout = 26,
        WorkoutStep = 27
    }

    public enum FITSports
    {
        Other = 0,
        Running,
        Cycling,
    }

    public enum FITFileIdFieldsIds
    {
        FileType = 0,
        ManufacturerId,
        ProductId,
        SerialNumber,
        ExportDate,
    }

    public enum FITWorkoutFieldIds
    {
        SportType = 4,
        Capabilities,
        NumSteps,
        WorkoutName = 8,
    }

    public enum FITWorkoutStepFieldIds
    {
        MessageIndex = 254,
        StepName = 0,
        DurationType,
        DurationValue,
        TargetType,
        TargetValue,
        TargetCustomValueLow,
        TargetCustomValueHigh,
        Intensity
    }

    public enum FITWorkoutStepIntensity
    {
        Active = 0,
        Rest,
        Warmup,
        Cooldown
    }

    public enum FITWorkoutStepDurationTypes
    {
        Time = 0,
        Distance,
        HeartRateLessThan,
        HeartRateGreaterThan,
        Calories,
        Open,
        RepeatCount,
        RepeatUntilTime,
        RepeatUntilDistance,
        RepeatUntilCalories,
        RepeatUntilHeartRateLessThan,
        RepeatUntilHeartRateGreaterThan,
        RepeatUntilPowerLessThan,
        RepeatUntilPowerGreaterThan,
        PowerLessThan,
        PowerGreaterThan,
    }

    public enum FITWorkoutStepTargetTypes
    {
        Speed,
        HeartRate,
        NoTarget,
        Cadence,
        Power,
        Grade,
        Resistance,
    }
}