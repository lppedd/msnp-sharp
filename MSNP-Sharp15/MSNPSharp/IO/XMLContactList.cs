﻿namespace MSNPSharp.IO
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    using MemberRole = MSNPSharp.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNABSharingService.ServiceFilterType;
using MSNPSharp.MSNABSharingService;

    /// <summary>
    /// ContactList file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("ContactList")]
    public class XMLContactList:MCLSerializer
    {
       
        public static XMLContactList LoadFromFile(string filename, bool nocompress)
        {
            return LoadFromFile(filename, nocompress, typeof(XMLContactList)) as XMLContactList;
        }


        #region Membership

        DateTime msLastChange;
        SerializableDictionary<int, Service> services = new SerializableDictionary<int, Service>(0);
        SerializableDictionary<string, MembershipContactInfo> mscontacts = new SerializableDictionary<string, MembershipContactInfo>(0);

        public DateTime MembershipLastChange
        {
            get
            {
                return msLastChange;
            }
            set
            {
                msLastChange = value;
            }
        }

        public SerializableDictionary<int, Service> Services
        {
            get
            {
                return services;
            }

            set
            {
                services = value;
            }
        }

        public SerializableDictionary<string, MembershipContactInfo> MembershipContacts
        {
            get
            {
                return mscontacts;
            }
            set
            {
                mscontacts = value;
            }
        }

        public MSNLists GetMSNLists(string account)
        {
            MSNLists contactlists = MSNLists.None;
            if (MembershipContacts.ContainsKey(account))
            {
                MembershipContactInfo ci = MembershipContacts[account];
                if (ci.Memberships.ContainsKey(MemberRole.Allow))
                    contactlists |= MSNLists.AllowedList;

                if (ci.Memberships.ContainsKey(MemberRole.Pending))
                    contactlists |= MSNLists.PendingList;

                if (ci.Memberships.ContainsKey(MemberRole.Reverse))
                    contactlists |= MSNLists.ReverseList;

                if (ci.Memberships.ContainsKey(MemberRole.Block))
                {
                    contactlists |= MSNLists.BlockedList;

                    if ((contactlists & MSNLists.AllowedList) == MSNLists.AllowedList)
                    {
                        contactlists ^= MSNLists.AllowedList;
                        RemoveMemberhip(account, MemberRole.Allow);
                    }
                }
            }
            return contactlists;
        }

        public void AddMemberhip(string account, ClientType type, MemberRole memberrole, int membershipid)
        {
            if (!MembershipContacts.ContainsKey(account))
                MembershipContacts.Add(account, new MembershipContactInfo(account, type));

            MembershipContacts[account].Type = type;
            MembershipContacts[account].Memberships[memberrole] = membershipid;
        }

        public void RemoveMemberhip(string account, MemberRole memberrole)
        {
            if (MembershipContacts.ContainsKey(account))
            {
                MembershipContacts[account].Memberships.Remove(memberrole);

                if (0 == MembershipContacts[account].Memberships.Count)
                    MembershipContacts.Remove(account);
            }
        }

        public virtual void Add(Dictionary<string, MembershipContactInfo> range)
        {
            foreach (string account in range.Keys)
            {
                if (mscontacts.ContainsKey(account))
                {
                    if (mscontacts[account].LastChanged.CompareTo(range[account].LastChanged) <= 0)
                    {
                        mscontacts[account] = range[account];
                    }
                }
                else
                {
                    mscontacts.Add(account, range[account]);
                }
            }
        }

        /// <summary>
        /// Combine the new services with old ones and get Messenger service's lastchange property.
        /// </summary>
        /// <param name="serviceRange"></param>
        public void CombineService(Dictionary<int, Service> serviceRange)
        {
            foreach (Service service in serviceRange.Values)
                services[service.Id] = service;

            foreach (Service innerService in Services.Values)
                if (innerService.Type == ServiceFilterType.Messenger)
                    if (msLastChange.CompareTo(innerService.LastChange) < 0)
                        msLastChange = innerService.LastChange;

        }

        /// <summary>
        /// Merge changes into membership list
        /// </summary>
        /// <param name="findMembership"></param>
        public void Merge(FindMembershipResultType findMembership)
        {
            if (null != findMembership && null != findMembership.Services)
            {
                Dictionary<int, Service> services = new Dictionary<int, Service>(0);

                foreach (ServiceType serviceType in findMembership.Services)
                {
                    Service currentService = new Service();
                    currentService.LastChange = serviceType.LastChange;
                    currentService.Id = int.Parse(serviceType.Info.Handle.Id);
                    currentService.Type = serviceType.Info.Handle.Type;
                    currentService.ForeignId = serviceType.Info.Handle.ForeignId;
                    services[currentService.Id] = currentService;

                    if (null != serviceType.Memberships)
                    {
                        foreach (Membership membership in serviceType.Memberships)
                        {
                            if (null != membership.Members)
                            {
                                MemberRole memberrole = membership.MemberRole;
                                foreach (BaseMember bm in membership.Members)
                                {
                                    string account = null;
                                    ClientType type = ClientType.PassportMember;

                                    if (bm is PassportMember)
                                    {
                                        type = ClientType.PassportMember;
                                        PassportMember pm = (PassportMember)bm;
                                        if (!pm.IsPassportNameHidden)
                                        {
                                            account = pm.PassportName;
                                        }
                                    }
                                    else if (bm is EmailMember)
                                    {
                                        type = ClientType.EmailMember;
                                        account = ((EmailMember)bm).Email;
                                    }
                                    else if (bm is PhoneMember)
                                    {
                                        type = ClientType.PhoneMember;
                                        account = ((PhoneMember)bm).PhoneNumber;
                                    }

                                    if (account != null)
                                    {
                                        account = account.ToLower(CultureInfo.InvariantCulture);

                                        if (bm.Deleted)
                                        {
                                            RemoveMemberhip(account, memberrole);
                                        }
                                        else
                                        {
                                            AddMemberhip(account, type, memberrole, Convert.ToInt32(bm.MembershipId));
                                            MembershipContacts[account].LastChanged = bm.LastChanged;
                                            MembershipContacts[account].DisplayName = String.IsNullOrEmpty(bm.DisplayName) ? account : bm.DisplayName;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Combine all membership information.
                CombineService(services);
            }
        }

        #endregion

        #region Addressbook

        DateTime abLastChange;
        DateTime dynamicItemLastChange;
        SerializableDictionary<string, string> myproperties = new SerializableDictionary<string, string>(0);
        SerializableDictionary<string, GroupInfo> groups = new SerializableDictionary<string, GroupInfo>(0);
        SerializableDictionary<Guid, AddressbookContactInfo> abcontacts = new SerializableDictionary<Guid, AddressbookContactInfo>(0);

        public DateTime AddressbookLastChange
        {
            get
            {
                return abLastChange;
            }
            set
            {
                abLastChange = value;
            }
        }

        public DateTime DynamicItemLastChange
        {
            get
            {
                return dynamicItemLastChange;
            }
            set
            {
                dynamicItemLastChange = value;
            }
        }

        public SerializableDictionary<string, string> MyProperties
        {
            get
            {
                return myproperties;
            }
            set
            {
                myproperties = value;
            }
        }

        public SerializableDictionary<string, GroupInfo> Groups
        {
            get
            {
                return groups;
            }

            set
            {
                groups = value;
            }
        }

        public SerializableDictionary<Guid, AddressbookContactInfo> AddressbookContacts
        {
            get
            {
                return abcontacts;
            }
            set
            {
                abcontacts = value;
            }
        }

        public void AddGroup(Dictionary<string, GroupInfo> range)
        {
            foreach (GroupInfo group in range.Values)
            {
                AddGroup(group);
            }
        }

        public void AddGroup(GroupInfo group)
        {
            if (groups.ContainsKey(group.Guid))
            {
                groups[group.Guid] = group;
            }
            else
            {
                groups.Add(group.Guid, group);
            }
        }

        public virtual void Add(Dictionary<Guid, AddressbookContactInfo> range)
        {
            foreach (Guid guid in range.Keys)
            {
                if (abcontacts.ContainsKey(guid))
                {
                    if (abcontacts[guid].LastChanged.CompareTo(range[guid].LastChanged) <= 0)
                    {
                        abcontacts[guid] = range[guid];
                    }
                }
                else
                {
                    abcontacts.Add(guid, range[guid]);
                }
            }
        }

        public virtual AddressbookContactInfo Find(string email, ClientType type)
        {
            foreach (AddressbookContactInfo ci in AddressbookContacts.Values)
            {
                if (ci.Account == email && ci.Type == type)
                    return ci;
            }
            return null;
        }

        /// <summary>
        /// Merge changes to addressbook and add contacts
        /// </summary>
        /// <param name="forwardList"></param>
        /// <param name="nsMessageHandler"></param>
        public void Merge(ABFindAllResultType forwardList, NSMessageHandler nsMessageHandler)
        {
            if (null != forwardList.contacts)
            {
                foreach (ContactType contactType in forwardList.contacts)
                {
                    if (null != contactType.contactInfo)
                    {
                        ClientType type = ClientType.PassportMember;
                        string account = contactType.contactInfo.passportName;
                        string displayname = contactType.contactInfo.displayName;
                        bool ismessengeruser = contactType.contactInfo.isMessengerUser;

                        if (contactType.contactInfo.emails != null && account == null)
                        {
                            /* This is not related with ClientCapacities, I think :)
                            if (Enum.IsDefined(typeof(ClientCapacities), long.Parse(contactType.contactInfo.emails[0].Capability)))
                            {
                                ci.Capability = (ClientCapacities)long.Parse(contactType.contactInfo.emails[0].Capability);
                            }
                             * */

                            type = ClientType.EmailMember;
                            account = contactType.contactInfo.emails[0].email;
                            ismessengeruser |= contactType.contactInfo.emails[0].isMessengerEnabled;
                            displayname = String.IsNullOrEmpty(contactType.contactInfo.quickName) ? account : contactType.contactInfo.quickName;
                        }

                        if (account == null)
                            continue; // PassportnameHidden... Nothing to do...

                        if (contactType.fDeleted)
                        {
                            AddressbookContacts.Remove(new Guid(contactType.contactId));
                        }
                        else
                        {
                            AddressbookContactInfo ci = new AddressbookContactInfo(account, type, new Guid(contactType.contactId));
                            ci.DisplayName = displayname;
                            ci.IsMessengerUser = ismessengeruser;
                            ci.LastChanged = contactType.lastChange;

                            if (null != contactType.contactInfo.annotations)
                            {
                                foreach (Annotation anno in contactType.contactInfo.annotations)
                                {
                                    if (anno.Name == "AB.NickName" && anno.Value != null)
                                    {
                                        ci.DisplayName = anno.Value;
                                        break;
                                    }
                                }
                            }

                            if (contactType.contactInfo.groupIds != null)
                            {
                                ci.Groups = new List<string>(contactType.contactInfo.groupIds);
                            }

                            if (contactType.contactInfo.contactType == contactInfoTypeContactType.Me)
                            {
                                if (ci.DisplayName == nsMessageHandler.Owner.Mail && nsMessageHandler.Owner.Name != String.Empty)
                                {
                                    ci.DisplayName = nsMessageHandler.Owner.Name;
                                }

                                MyProperties["displayname"] = ci.DisplayName;
                                MyProperties["cid"] = contactType.contactInfo.CID;

                                if (null != contactType.contactInfo.annotations)
                                {
                                    foreach (Annotation anno in contactType.contactInfo.annotations)
                                    {
                                        string name = anno.Name;
                                        string value = anno.Value;
                                        name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                                        MyProperties[name] = value;
                                    }
                                }

                                if (!MyProperties.ContainsKey("mbea"))
                                    MyProperties["mbea"] = "0";

                                if (!MyProperties.ContainsKey("gtc"))
                                    MyProperties["gtc"] = "1";

                                if (!MyProperties.ContainsKey("blp"))
                                    MyProperties["blp"] = "0";

                                if (!MyProperties.ContainsKey("roamliveproperties"))
                                    MyProperties["roamliveproperties"] = "1";

                                if (!MyProperties.ContainsKey("personalmessage"))
                                    MyProperties["personalmessage"] = MyProperties["displayname"];
                            }

                            if (AddressbookContacts.ContainsKey(ci.Guid))
                            {
                                if (AddressbookContacts[ci.Guid].LastChanged.CompareTo(ci.LastChanged) < 0)
                                {
                                    AddressbookContacts[ci.Guid] = ci;
                                }
                            }
                            else
                            {
                                AddressbookContacts.Add(ci.Guid, ci);
                            }
                        }
                    }
                }
            }

            if (null != forwardList.groups)
            {
                foreach (GroupType groupType in forwardList.groups)
                {
                    if (groupType.fDeleted)
                    {
                        Groups.Remove(groupType.groupId);
                    }
                    else
                    {
                        GroupInfo group = new GroupInfo();
                        group.Name = groupType.groupInfo.name;
                        group.Guid = groupType.groupId;
                        Groups[group.Guid] = group;
                    }
                }
            }

            // Refresh lastchange
            AddressbookLastChange = forwardList.ab.lastChange;
            DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;

            // Create Groups
            foreach (GroupInfo group in Groups.Values)
            {
                nsMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.Name, group.Guid, nsMessageHandler));
            }

            // Create the messenger contacts
            if (MembershipContacts.Count > 0)
            {
                foreach (MembershipContactInfo ci in MembershipContacts.Values)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(ci.Account, ci.DisplayName);
                    contact.SetLists(GetMSNLists(ci.Account));
                    contact.SetClientType(MembershipContacts[ci.Account].Type);
                    contact.NSMessageHandler = nsMessageHandler;

                    AddressbookContactInfo abci = Find(ci.Account, ci.Type);
                    if (abci != null)
                    {
                        contact.SetGuid(abci.Guid);
                        contact.SetIsMessengerUser(abci.IsMessengerUser);
                        if (abci.IsMessengerUser)
                        {
                            contact.AddToList(MSNLists.ForwardList); //IsMessengerUser is only valid in AddressBook member
                        }

                        foreach (string groupId in abci.Groups)
                        {
                            contact.ContactGroups.Add(nsMessageHandler.ContactGroups[groupId]);
                        }

                        if (abci.Type == ClientType.EmailMember)
                        {
                            contact.ClientCapacities = abci.Capability;
                        }

                        contact.SetName((abci.LastChanged.CompareTo(ci.LastChanged) < 0 && String.IsNullOrEmpty(abci.DisplayName)) ? ci.DisplayName : abci.DisplayName);
                    }
                }
            }

            // Create the mail contacts on your forward list
            foreach (AddressbookContactInfo abci in AddressbookContacts.Values)
            {
                if (!MembershipContacts.ContainsKey(abci.Account) && abci.Account != nsMessageHandler.Owner.Mail)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(abci.Account, abci.DisplayName);
                    contact.SetGuid(abci.Guid);
                    contact.SetIsMessengerUser(abci.IsMessengerUser);
                    contact.SetClientType(abci.Type);
                    contact.NSMessageHandler = nsMessageHandler;

                    foreach (string groupId in abci.Groups)
                    {
                        contact.ContactGroups.Add(nsMessageHandler.ContactGroups[groupId]);
                    }

                    if (abci.Type == ClientType.EmailMember)
                    {
                        contact.ClientCapacities = abci.Capability;
                    }
                }
            }
        }
        #endregion
    }
};
