// This software is open source software available under the BSD-3 license.
// 
// Copyright (c) 2018, Triad National Security, LLC
// All rights reserved.
// 
// Copyright 2018. Triad National Security, LLC. This software was produced under U.S. Government contract 89233218CNA000001 for Los Alamos National Laboratory (LANL), which is operated by Triad National Security, LLC for the U.S. Department of Energy. The U.S. Government has rights to use, reproduce, and distribute this software.  NEITHER THE GOVERNMENT NOR TRIAD NATIONAL SECURITY, LLC MAKES ANY WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS SOFTWARE.  If software is modified to produce derivative works, such modified software should be clearly marked, so as not to confuse it with the version available from LANL.
// 
// Additionally, redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 1.       Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
// 2.       Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
// 3.       Neither the name of Triad National Security, LLC, Los Alamos National Laboratory, LANL, the U.S. Government, nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission. 
//  
// THIS SOFTWARE IS PROVIDED BY TRIAD NATIONAL SECURITY, LLC AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL TRIAD NATIONAL SECURITY, LLC OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace Omniscient
{
    public enum ParameterType { String, Int, Double, Bool, Enum, TimeSpan, DateTimeFormat, SystemChannel, SystemEventGenerator, FileName, Directory, InstrumentChannel }

    /// <summary>
    /// A description of a Parameter. Used in Hookups
    /// </summary>
    public class ParameterTemplate
    {
        public string Name { get; private set; }
        public ParameterType Type { get; private set; }
        public List<string> ValidValues { get; private set; }
        public ParameterTemplate(string name, ParameterType type)
        {
            Name = name;
            Type = type;
            ValidValues = new List<string>();
        }
        public ParameterTemplate(string name, ParameterType type, List<string> validValues)
        {
            Name = name;
            Type = type;
            ValidValues = validValues;
        }
    }

    /// <summary>
    /// The base class for all Parameter classes.
    /// </summary>
    /// <remarks>
    /// The idea behind Parameters is to make it easier to add new Instruments
    /// and EventGenerators to Omniscient. It will simplify the way 
    /// Omniscient's GUI works by allowing standard interfaces for Instrument
    /// and EventGenerator parameters.
    /// </remarks>
    public abstract class Parameter
    {
        public static List<Parameter> FromXML(XmlNode node, List<ParameterTemplate> templates, DetectionSystem system=null, Instrument instrument=null)
        {
            List<Parameter> parameters = new List<Parameter>();

            foreach (ParameterTemplate pTemplate in templates)
            {
                string paramNameStr = pTemplate.Name.Replace(' ', '_');
                switch (pTemplate.Type)
                {
                    case ParameterType.String:
                        parameters.Add(new StringParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.Int:
                        parameters.Add(new IntParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.Double:
                        parameters.Add(new DoubleParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.Bool:
                        parameters.Add(new BoolParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.Enum:
                        parameters.Add(new EnumParameter(pTemplate.Name)
                        {
                            Value = node.Attributes[paramNameStr]?.InnerText,
                            ValidValues = pTemplate.ValidValues
                        });
                        break;
                    case ParameterType.SystemChannel:
                        parameters.Add(new SystemChannelParameter(pTemplate.Name, system) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.SystemEventGenerator:
                        parameters.Add(new SystemEventGeneratorParameter(pTemplate.Name, system) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.TimeSpan:
                        parameters.Add(new TimeSpanParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.DateTimeFormat:
                        parameters.Add(new DateTimeFormatParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.FileName:
                        parameters.Add(new FileNameParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.Directory:
                        parameters.Add(new DirectoryParameter(pTemplate.Name) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                    case ParameterType.InstrumentChannel:
                        parameters.Add(new InstrumentChannelParameter(pTemplate.Name, instrument) { Value = node.Attributes[paramNameStr]?.InnerText });
                        break;
                }
            }
            return parameters;
        }
        public ParameterType Type { get; private set; }

        public string Name { get; set; }
        public string Value { get; set; }

        public Parameter(string name, ParameterType type)
        {
            Value = "";
            Name = name;
            Type = type;
        }
        public abstract bool Validate();
    }

    /// <summary>
    /// Base class for all Parameter classes that have a limited set of valid
    /// values.
    /// </summary>
    public abstract class LimitedValueParameter : Parameter
    {
        public List<string> ValidValues { get; set; }

        public LimitedValueParameter(string name, ParameterType type) : base(name, type) { }

        public override bool Validate()
        {
            foreach (string validValue in ValidValues)
            {
                if (Value == validValue) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A simple Parameter for a string. 
    /// More specifc Parameter types should be used when available.
    /// </summary>
    public class StringParameter : Parameter
    {
        public StringParameter(string name) : base(name, ParameterType.String) { }
        public override bool Validate() { return true; }
    }

    /// <summary>
    /// A simple Parameter for an integer.
    /// </summary>
    public class IntParameter : Parameter
    {
        public IntParameter(string name) : base(name, ParameterType.Int) { }
        public override bool Validate() { return int.TryParse(Value, out int result); }
        public int ToInt() { return int.Parse(Value); }
    }

    /// <summary>
    /// A simple Parameter for a double (i.e. a floating point number).
    /// </summary>
    public class DoubleParameter : Parameter
    {
        public DoubleParameter(string name) : base(name, ParameterType.Double) { }
        public override bool Validate() { return double.TryParse(Value, out double result); }
        public double ToDouble() { return double.Parse(Value); }
    }

    /// <summary>
    /// A Parameter that has only two possible values: True or False.
    /// </summary>
    public class BoolParameter : Parameter
    {
        public static readonly string True = "True";
        public static readonly string False = "False";
        public BoolParameter(string name, bool initialValue=false) : base(name, ParameterType.Bool)
        {
            Set(initialValue);
        }

        public override bool Validate()
        {
            if (Value != True && Value != False) return false;
            return true;
        }
        public void Set(bool value)
        {
            if (value) Value = True;
            else Value = False;
        }
        public bool ToBool()
        {
            if (Value == True) return true;
            return false;
        }
    }

    /// <summary>
    /// An EnumParameter is a parameter that can take on a fixed set of values.
    /// </summary>
    public class EnumParameter : LimitedValueParameter
    {
        public EnumParameter(string name) : base(name, ParameterType.Enum)
        {
            ValidValues = new List<string>();
        }

        public int ToInt()
        {
            for (int i = 0; i < ValidValues.Count; i++)
            {
                if (Value == ValidValues[i]) return i;
            }
            return -1;
        }
    }

    /// <summary>
    /// A TimeSpanParameter is internally stored as a string of a double 
    /// representing seconds.
    /// </summary>
    public class TimeSpanParameter : Parameter
    {
        public TimeSpanParameter(string name) : base(name, ParameterType.TimeSpan) {}

        public override bool Validate() { return double.TryParse(Value, out double result); }

        public TimeSpan ToTimeSpan()
        {
            return TimeSpan.FromSeconds(double.Parse(Value));
        }
    }

    /// <summary>
    /// A SystemChannelParameter is a Channel within a particular 
    /// DetectionSystem.
    /// </summary>
    public class SystemChannelParameter : LimitedValueParameter
    {
        public DetectionSystem System { get; set; }

        public SystemChannelParameter(string name, DetectionSystem system) : base(name, ParameterType.SystemChannel)
        {
            System = system;
            ValidValues = new List<string>();
            foreach (Instrument inst in System.GetInstruments())
            {
                foreach (Channel ch in inst.GetChannels())
                {
                    ValidValues.Add(ch.Name);
                }
            }
        }

        public Channel ToChannel()
        {
            foreach (Instrument inst in System.GetInstruments())
            {
                foreach (Channel ch in inst.GetChannels())
                {
                    if (Value == ch.Name) return ch;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// A parameter that stores DateTime formats and provides regex patterns 
    /// to extract DateTimes from strings
    /// </summary>
    public class DateTimeFormatParameter : LimitedValueParameter
    {
        private static Dictionary<string, string> patterns = new Dictionary<string, string>
        {
            { "yyyy-MM-dd HH:mm:ss",  @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}" },
            { "yyyy-MM-ddTHH:mm:ss",  @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}" },
            { "yyyyMMddTHHmmss",      @"\d{8}T\d{6}" }
        };

        public DateTimeFormatParameter(string name) : base(name, ParameterType.DateTimeFormat)
        {
            ValidValues = patterns.Keys.ToList();
        }

        public string GetRegexPattern()
        {
            if (!(Value is null)) return patterns[Value];
            return null;
        }
    }

    /// <summary>
    /// A Parameter for an EventGenerator within a particular DetectionSystem
    /// </summary>
    public class SystemEventGeneratorParameter : LimitedValueParameter
    {
        DetectionSystem System { get; set; }
        public SystemEventGeneratorParameter(string name, DetectionSystem system) : base(name, ParameterType.SystemEventGenerator)
        {
            System = system;
            ValidValues = new List<string>();
            foreach (EventGenerator eg in System.GetEventGenerators())
            {
                ValidValues.Add(eg.Name);
            }
        }

        public EventGenerator ToEventGenerator()
        {
            foreach (EventGenerator eg in System.GetEventGenerators())
            {
                if (Value == eg.Name) return eg;
            }
            return null;
        }
    }

    /// <summary>
    /// A FileNameParameter stores the name of a file on the system.
    /// </summary>
    public class FileNameParameter : Parameter
    {
        public FileNameParameter(string name) : base(name, ParameterType.FileName) {}

        public override bool Validate()
        {
            if (File.Exists(Value)) return true;
            return false;
        }
    }

    /// <summary>
    /// A DirectoryParameter stores the name of a directoy on the system.
    /// </summary>
    public class DirectoryParameter : Parameter
    {
        public DirectoryParameter(string name) : base(name, ParameterType.Directory) { }

        public override bool Validate()
        {
            if (Directory.Exists(Value)) return true;
            return false;
        }
    }

    /// <summary>
    /// An InstrumentChannelParameter is a Channel within a particular 
    /// Instrument.
    /// </summary>
    public class InstrumentChannelParameter : LimitedValueParameter
    {
        public Instrument Instrument { get; set; }

        public InstrumentChannelParameter(string name, Instrument inst, int maxChannel=int.MaxValue) : base(name, ParameterType.InstrumentChannel)
        {
            Instrument = inst;
            ValidValues = new List<string>();
            Channel[] channels = Instrument.GetChannels();
            int nChannels = channels.Length;
            if (maxChannel > nChannels - 1) maxChannel = nChannels - 1;
            for (int i=0; i<=maxChannel; i++)
            {
                ValidValues.Add(channels[i].Name);
            }
        }

        public Channel ToChannel()
        {
            foreach (Channel ch in Instrument.GetChannels())
            {
                if (Value == ch.Name) return ch;
            }
            return null;
        }
    }
}
