/*****************************************************************************************
 *  p2p-player
 *  An audio player developed in C# based on a shared base to obtain the music from.
 * 
 *  Copyright (C) 2010-2011 Dario Mazza, Sebastiano Merlino
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Dario Mazza (dariomzz@gmail.com)
 *  Sebastiano Merlino (etr@pensieroartificiale.com)
 *  Full Source and Documentation available on Google Code Project "p2p-player", 
 *  see <http://code.google.com/p/p2p-player/>
 *
 ******************************************************************************************/

using System.Text;
using System.Runtime.Serialization;

namespace Persistence
{
	namespace Tag
	{
		[DataContractAttribute]
		public class CompleteTag
		{
			/// <summary>
			/// Tag Default constructor.
			/// </summary>
			public CompleteTag() { }

			/// <summary>
			/// Method used for Serialization purpose
			/// </summary>
			/// <param name="info"></param>
			/// <param name="ctxt"></param>
			public CompleteTag(SerializationInfo info, StreamingContext ctxt)
			{
				Value = (string)info.GetValue("Value", typeof(string));
				FileHash = (string)info.GetValue("FileHash", typeof(string));
				TagHash = (string)info.GetValue("TagHash", typeof(string));
			}

			[DataMemberAttribute]
			public string Value
			{
				get;
				set;
			}

			[DataMemberAttribute]
			public string FileHash
			{
				get;
				set;
			}

			[DataMemberAttribute]
			public string TagHash
			{
				get;
				set;
			}

			/// <summary>
			/// Method used for serialization purpose.
			/// </summary>
			/// <param name="info"></param>
			/// <param name="context"></param>
			public void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("Value", Value);
				info.AddValue("FileHash", FileHash);
				info.AddValue("TagHash", TagHash);
			}
		}
	}
}//namespace Persistence.Tag