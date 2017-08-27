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

using System;
using System.Security.Cryptography;
using System.Text;

namespace Persistence
{
	namespace Tag
	{
		public class CompleteTag
		{
			public string Key { get; set; }
			public string Value { get; set; }
			public string Hash { get; set; }

			public CompleteTag() { }

			public CompleteTag(string key, string val)
			{
				Key = key;
				Value = val;
				ComputeHash(key);
			}

			protected void ComputeHash(string key)
			{
				//var algo = SHA256.Create();
				//byte[] hash = algo.ComputeHash(Encoding.ASCII.GetBytes(key));
				//Hash = Convert.ToBase64String(hash);
				HashAlgorithm hasher = new SHA1CryptoServiceProvider(); // Keeping this around results in exceptions
				byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(key));
				StringBuilder sb = new StringBuilder();

				for (int i = 0; i < hash.Length; i++)
				{
					sb.Append(hash[i].ToString("x2"));
				}

				Hash = sb.ToString();
			}
		}
	}
}