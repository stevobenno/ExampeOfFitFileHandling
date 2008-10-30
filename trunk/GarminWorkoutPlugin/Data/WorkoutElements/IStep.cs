using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using ZoneFiveSoftware.Common.Data.Fitness;
using GarminFitnessPlugin.Controller;

namespace GarminFitnessPlugin.Data
{
    abstract class IStep : IPluginSerializable, IXMLSerializable, IDirty
    {
        protected IStep(StepType type, Workout parent)
        {
            m_StepType = type;
            m_ParentWorkout = parent;
        }

        public enum StepType
        {
            Regular = 0,
            Repeat,
            StepTypeCount
        }

        public override void Serialize(Stream stream)
        {
            // Type
            stream.Write(BitConverter.GetBytes((Int32)Type), 0, sizeof(Int32));

            // Notes
            if (Notes != null && Notes != String.Empty)
            {
                stream.Write(BitConverter.GetBytes(Encoding.UTF8.GetByteCount(Notes)), 0, sizeof(Int32));
                stream.Write(Encoding.UTF8.GetBytes(Notes), 0, Encoding.UTF8.GetByteCount(Notes));
            }
            else
            {
                stream.Write(BitConverter.GetBytes((Int32)0), 0, sizeof(Int32));
            }
        }

        public void Deserialize_V0(Stream stream, DataVersion version)
        {
        }

        public void Deserialize_V6(Stream stream, DataVersion version)
        {
            byte[] intBuffer = new byte[sizeof(Int32)];
            byte[] stringBuffer;
            Int32 stringLength;

            // Notes
            stream.Read(intBuffer, 0, sizeof(Int32));
            stringLength = BitConverter.ToInt32(intBuffer, 0);

            if (stringLength > 0)
            {
                stringBuffer = new byte[stringLength];
                stream.Read(stringBuffer, 0, stringLength);
                Notes = Encoding.UTF8.GetString(stringBuffer);
            }
            else
            {
                Notes = String.Empty;
            }
        }

        public virtual void Serialize(XmlNode parentNode, XmlDocument document)
        {
            XmlAttribute attribute = document.CreateAttribute("xsi", "type", Constants.xsins);

            attribute.Value = Constants.StepTypeTCXString[(int)Type];
            parentNode.Attributes.Append(attribute);

            XmlNode idNode = document.CreateElement("StepId");
            idNode.AppendChild(document.CreateTextNode(Utils.GetStepExportId(this).ToString()));
            parentNode.AppendChild(idNode);

            // Extension
            XmlNode valueNode;
            XmlNode extensionNode;

            extensionNode = document.CreateElement("StepNotes");
            valueNode = document.CreateElement("StepId");
            valueNode.AppendChild(document.CreateTextNode(Utils.GetStepExportId(this).ToString()));
            extensionNode.AppendChild(valueNode);
            valueNode = document.CreateElement("Notes");
            valueNode.AppendChild(document.CreateTextNode(Notes));
            extensionNode.AppendChild(valueNode);

            ParentWorkout.AddSportTracksExtension(extensionNode);
        }

        public virtual bool Deserialize(XmlNode parentNode)
        {
            return true;
        }

        public virtual byte GetStepCount()
        {
            return 1;
        }

        public abstract IStep Clone();
        public abstract bool ValidateAfterZoneCategoryChanged(IZoneCategory changedCategory);

        public StepType Type
        {
            get { return m_StepType; }
        }

        public Workout ParentWorkout
        {
            get { return m_ParentWorkout; }
        }

        public string Notes
        {
            get { return m_Notes; }
            set { m_Notes = value; }
        }

        public abstract bool IsDirty
        {
            get;
            set;
        }

        private StepType m_StepType;
        private Workout m_ParentWorkout;
        private string m_Notes = String.Empty;
    }
}
