#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MemberRole = MSNPSharp.MSNWS.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNWS.MSNABSharingService.ServiceFilterType;

    #region Contact Types

    #region ContactInfo

    [Serializable]
    public class ContactInfo
    {
        protected ContactInfo()
        {
        }

        private string account;
        public string Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        private ClientType type = ClientType.PassportMember;
        public ClientType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        private string displayname;
        public string DisplayName
        {
            get
            {
                return displayname;
            }
            set
            {
                displayname = value;
            }
        }

        private DateTime lastchanged;
        public DateTime LastChanged
        {
            get
            {
                return lastchanged;
            }
            set
            {
                lastchanged = value;
            }
        }

        private ClientCapacities capability = 0;
        public ClientCapacities Capability
        {
            get
            {
                return capability;
            }
            set
            {
                capability = value;
            }
        }

        private object tags;

        /// <summary>
        /// Save whatever you want
        /// </summary>
        public object Tags
        {
            get
            {
                return tags;
            }
            set
            {
                tags = value;
            }
        }

        /// <summary>
        /// The string for this instance
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugstr = String.Empty;

            if (account != null)
                debugstr += account + " | " + Type.ToString();

            if (displayname != null)
                debugstr += " | " + displayname;

            return debugstr;
        }

        /// <summary>
        /// Overrided. Treat contacts with same account but different clienttype as different contacts.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            ContactInfo cinfo = obj as ContactInfo;
            return ((Account.ToLowerInvariant() == cinfo.Account.ToLowerInvariant()) && (Type == cinfo.Type));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion

    /// <summary>
    /// Contact type for membership list
    /// </summary>
    [Serializable]
    public class MembershipContactInfo : ContactInfo
    {
        protected MembershipContactInfo()
        {
        }

        public MembershipContactInfo(string account, ClientType type)
        {
            Account = account;
            Type = type;
        }

        private SerializableDictionary<MemberRole, int> memberships = new SerializableDictionary<MemberRole, int>();
        public SerializableDictionary<MemberRole, int> Memberships
        {
            get
            {
                return memberships;
            }
            set
            {
                memberships = value;
            }
        }
    }

    #endregion

    #region Service
    /// <summary>
    /// Membership service
    /// </summary>
    [Serializable]
    public class Service
    {
        private int id;
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        private ServiceFilterType serviceType;
        public ServiceFilterType ServiceType
        {
            get
            {
                return serviceType;
            }
            set
            {
                serviceType = value;
            }
        }

        private DateTime lastChange;
        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
            set
            {
                lastChange = value;
            }
        }

        private string foreignId;
        public string ForeignId
        {
            get
            {
                return foreignId;
            }
            set
            {
                foreignId = value;
            }
        }

        public override string ToString()
        {
            return Convert.ToString(ServiceType);
        }
    }

    #endregion

    #region Owner properties

    /// <summary>
    /// Base class for profile resource
    /// </summary>
    [Serializable]
    public class ProfileResource
    {
        private DateTime dateModified;
        private string resourceID;

        /// <summary>
        /// Last modify time of the resource
        /// </summary>
        public DateTime DateModified
        {
            get
            {
                return dateModified;
            }
            set
            {
                dateModified = value;
            }
        }

        /// <summary>
        /// Identifier of the resource
        /// </summary>
        public string ResourceID
        {
            get
            {
                return resourceID;
            }
            set
            {
                resourceID = value;
            }
        }
    }

    /// <summary>
    /// Owner's photo resource in profile
    /// </summary>
    [Serializable]
    public class ProfilePhoto : ProfileResource
    {
        private string preAthURL;
        private SerializableMemoryStream displayImage;

        public string PreAthURL
        {
            get
            {
                return preAthURL;
            }
            set
            {
                preAthURL = value;
            }
        }

        public SerializableMemoryStream DisplayImage
        {
            get
            {
                return displayImage;
            }
            set
            {
                displayImage = value;
            }
        }
    }

    /// <summary>
    /// Owner profile
    /// </summary>
    [Serializable]
    public class OwnerProfile : ProfileResource
    {
        private string cID = string.Empty;
        private string displayName = string.Empty;
        private string personalMessage = string.Empty;
        private ProfilePhoto photo = new ProfilePhoto();
        
        public bool GetFromStorageService = true;

        /// <summary>
        /// DisplayImage of owner.
        /// </summary>
        public ProfilePhoto Photo
        {
            get
            {
                return photo;
            }
            set
            {
                photo = value;
            }
        }

        /// <summary>
        /// CID of owner.
        /// </summary>
        public string CID
        {
            get
            {
                return cID;
            }
            set
            {
                cID = value;
            }
        }

        /// <summary>
        /// DisplayName of owner
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        /// <summary>
        /// Personal description of owner.
        /// </summary>
        public string PersonalMessage
        {
            get
            {
                return personalMessage;
            }
            set
            {
                personalMessage = value;
            }
        }


    }
    #endregion
}