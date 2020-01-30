﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenProtocolInterpreter.MIDs {
	public abstract class MID : IMID {
		protected Dictionary<int, IEnumerable<DataField>> RevisionsByFields;
		protected IMID nextTemplate;

		public MID(Header header) {
			this.HeaderData = header;
			this.RegisteredDataFields = new List<DataField>();
			this.RevisionsByFields = new Dictionary<int, IEnumerable<DataField>>();
			this.loadRevisionsFields();
			this.registerDatafields();
		}

		public MID(int length, int MID, int revision, int? noAckFlag = null, int? spindleID = null, int? stationID = null, IEnumerable<DataField> usedAs = null) {
			this.HeaderData = new Header() {
				Length = length,
				Mid = MID,
				Revision = revision,
				NoAckFlag = noAckFlag,
				SpindleID = spindleID,
				StationID = stationID,
				UsedAs = usedAs
			};
			this.RegisteredDataFields = new List<DataField>();
			this.RevisionsByFields = new Dictionary<int, IEnumerable<DataField>>();
			this.loadRevisionsFields();
			this.registerDatafields();
		}

		public MID(int MID, int revision, int? noAckFlag = null, int? spindleID = null, int? stationID = null, IEnumerable<DataField> usedAs = null) {
			this.HeaderData = new Header() {
				Length = 20,
				Mid = MID,
				Revision = revision,
				NoAckFlag = noAckFlag,
				SpindleID = spindleID,
				StationID = stationID,
				UsedAs = usedAs
			};
			this.RegisteredDataFields = new List<DataField>();
			this.RevisionsByFields = new Dictionary<int, IEnumerable<DataField>>();
			this.loadRevisionsFields();
			this.registerDatafields();
		}

		public List<DataField> RegisteredDataFields { get; set; }

		public Header HeaderData { get; set; }

		protected bool isCorrectType(int mid) { return (mid == this.HeaderData.Mid); }

		protected bool isCorrectType(string package) {
			int mid;
			if (int.TryParse(package.Substring(4, 4), out mid))
				return (mid == this.HeaderData.Mid);

			return false;
		}

		protected virtual string buildHeader() { return this.HeaderData.ToString(); }

		public virtual string buildPackage() {
			string package = this.buildHeader();

			if (this.RegisteredDataFields.Count == 0)
				return package;

			for (int i = 1; i < this.RegisteredDataFields.Count + 1; i++)
				package += i.ToString().PadLeft(2, '0') + RegisteredDataFields[i - 1].getPaddedLeftValue();

			return package;
		}

		protected virtual void registerDatafields() {
			this.RegisteredDataFields.Clear();
			for (int i = 1; i <= this.HeaderData.Revision; i++)
				this.RegisteredDataFields.AddRange(this.RevisionsByFields[i]);
		}

		protected virtual void loadRevisionsFields() {

		}

		protected virtual Header processHeader(string package) {
			Header header = new Header {
				Length = Convert.ToInt32(package.Substring(0, 4)),
				Mid = Convert.ToInt32(package.Substring(4, 4)),
				Revision = (package.Length >= 11 && !string.IsNullOrWhiteSpace(package.Substring(8, 3))) ? Convert.ToInt32(package.Substring(8, 3)) : 1,
				NoAckFlag = (package.Length >= 12 && !string.IsNullOrWhiteSpace(package.Substring(11, 1))) ? (int?) Convert.ToInt32(package.Substring(11, 1)) : null,
				StationID = (package.Length >= 14 && !string.IsNullOrWhiteSpace(package.Substring(12, 2))) ? (int?) Convert.ToInt32(package.Substring(12, 2)) : null,
				SpindleID = (package.Length >= 16 && !string.IsNullOrWhiteSpace(package.Substring(14, 2))) ? (int?) Convert.ToInt32(package.Substring(14, 2)) : null
			};

			return header;
		}

		public virtual MID processPackage(string package) {
			this.HeaderData = this.processHeader(package);
			this.processDataFields(package);
			return this;
		}

		protected void processDataFields(string package) {
			int plen=string.IsNullOrEmpty(package)?0:package.Length,fldMaxPos;

			foreach (var dataField in this.RegisteredDataFields)
				try {
					if ((fldMaxPos=(2+dataField.Index+dataField.Size))>plen)
						Trace.WriteLine("data-field starting at "+(2+dataField.Index)+" ends out of range!");
					else
						dataField.Value = package.Substring(2 + dataField.Index, dataField.Size);
				} catch (ArgumentOutOfRangeException) {
					//null value
				}
		}

		protected void updateRevisionFromPackage(string package) {
			this.HeaderData.Revision = (!string.IsNullOrWhiteSpace(package.Substring(8, 3))) ? Convert.ToInt32(package.Substring(8, 3)) : this.RevisionsByFields.Keys.Max();
		}

		public class Header {
			public int Length { get; set; }
			public int Mid { get; set; }
			public int Revision { get; set; }
			public int? NoAckFlag { get; set; }
			public int? SpindleID { get; set; }
			public int? StationID { get; set; }
			public IEnumerable<DataField> UsedAs { get; set; }

			public override string ToString() {
				string header = string.Empty;
				header += this.Length.ToString().PadLeft(4, '0');
				header += this.Mid.ToString().PadLeft(4, '0');
				header += this.Revision.ToString().PadLeft(3, '0');
				header += this.NoAckFlag.ToString().PadLeft(1, ' ');
				header += (this.StationID != null) ? this.StationID.ToString().PadLeft(2, '0') : this.StationID.ToString().PadLeft(2, ' ');
				header += (this.SpindleID != null) ? this.SpindleID.ToString().PadLeft(2, '0') : this.SpindleID.ToString().PadLeft(2, ' ');
				string usedAs = "    ";
				if (UsedAs != null) {
					usedAs = string.Empty;
					foreach (DataField field in UsedAs)
						usedAs += field.Value.ToString();
				}
				header += usedAs;
				return header;
			}
		}

		internal void setNextTemplate(MID mid) {
			this.nextTemplate = mid;
		}
	}
}