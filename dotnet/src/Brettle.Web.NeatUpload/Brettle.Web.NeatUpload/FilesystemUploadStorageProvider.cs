/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.IO;
using System.Web;
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;

namespace Brettle.Web.NeatUpload
{
	public class FilesystemUploadStorageProvider : UploadStorageProvider
	{
		public override void Initialize(string providerName, NameValueCollection attrs)
		{
			this._name = providerName;
			TempDirectory = GetTempDirectory(attrs);
		}

		public override string Description { get { return "Streams uploads to disk."; } }

		private string _name = null;
		public override string Name { get { return _name; } }

		public override UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName, string contentType)
		{
			return this.CreateUploadedFile(context, controlUniqueID, fileName, contentType, null);
		}

		public override UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName, string contentType, UploadStorageConfig storageConfig)
		{
			return new FilesystemUploadedFile(this, controlUniqueID, fileName, contentType, storageConfig);
		}

        public DirectoryInfo TempDirectory = null;
		
		internal DirectoryInfo GetTempDirectory(NameValueCollection attrs)
		{
            string tempPath = null;
            if (attrs != null)
            {
                foreach (string name in attrs.Keys)
                {
                    string val = attrs[name];
                    if (name == "tempDirectory")
                    {
                        if (HttpContext.Current != null)
                        {
                            val = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath,
                                                    val);
                        }
                        tempPath = val;
                    }
                    else
                    {
                        throw new System.Xml.XmlException("Unrecognized attribute: " + name);
                    }
                }
            }
            if (tempPath != null)
            {
                return new DirectoryInfo(tempPath);
            }
            else if (TempDirectory != null)
            {
                return TempDirectory;
            }
            else
            {
                return GetDefaultTempDirectory();
            }
		}
		
		private DirectoryInfo GetDefaultTempDirectory()
		{
			// Use "app_data/NeatUpload_Temp" if it exists (or can be created), and is writable.  Otherwise use the
			// system temp dir.
			try
			{
				DirectoryInfo appDir = new DirectoryInfo(HttpContext.Current.Request.PhysicalApplicationPath);
				DirectoryInfo tempDir = appDir.CreateSubdirectory(Path.Combine("app_data", "NeatUpload_Temp"));
				string testFilePath = Path.Combine(tempDir.FullName, "test.config");
				FileStream s = File.Create(testFilePath);
				try
				{
					s.WriteByte(42);
				}
				finally
				{
					s.Close();
				}
				File.Delete(testFilePath);
				return tempDir;
			}
			catch (Exception)
			{
				return new DirectoryInfo(Path.GetTempPath());
			}
		}
		
	}
}
