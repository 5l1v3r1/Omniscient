﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Omniscient.Instruments
{
    abstract class Instrument
    {
        protected string name;
        protected string instrumentType;
        protected string dataFolder;

        protected int numChannels;
        protected Channel[] channels;

        public Instrument()
        {
            
        }

        public abstract void ScanDataFolder();
        public abstract void LoadData(DateTime startDate, DateTime endDate);

        public void SetDataFolder(string newDataFolder)
        {
            dataFolder = newDataFolder;
            ScanDataFolder();
        }

        public string GetName() { return name; }
        public string GetInstrumentType() { return instrumentType; }
        public string GetDataFolder() { return dataFolder; }
        public int GetNumChannels() { return numChannels; }
        public Channel GetChannel(int chanNum)
        {
            return channels[chanNum];
        }
    }
}