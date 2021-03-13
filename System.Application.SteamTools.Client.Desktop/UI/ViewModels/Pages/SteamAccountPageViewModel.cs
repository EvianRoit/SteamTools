﻿using ReactiveUI;
using System.Application.Models;
using System.Application.Models.Settings;
using System.Application.Services;
using System.Application.UI.Resx;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Properties;
using System.Threading.Tasks;
using System.Windows;

namespace System.Application.UI.ViewModels
{
    public class SteamAccountPageViewModel : TabItemViewModel
    {
        readonly ISteamService steamService = DI.Get<ISteamService>();
        readonly IHttpService httpService = DI.Get<IHttpService>();
        readonly ISteamworksWebApiService webApiService = DI.Get<ISteamworksWebApiService>();

        public override string Name
        {
            get => AppResources.UserFastChange;
            protected set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// steam记住的用户列表
        /// </summary>
        private IList<SteamUser>? _steamUsers;
        public IList<SteamUser>? SteamUsers
        {
            get => _steamUsers;
            set => this.RaiseAndSetIfChanged(ref _steamUsers, value);
        }

        internal async override Task Initialize()
        {
            SteamUsers = new ObservableCollection<SteamUser>(steamService.GetRememberUserList());

#if DEBUG
            for (var i = 0; i < 10; i++)
            {
                SteamUsers.Add(SteamUsers[0]);
            }
#endif

            if (!SteamUsers.Any_Nullable())
            {
                //Toast.Show("");
                return;
            }
            var users = SteamUsers.ToArray();
            for (var i = 0; i < SteamUsers.Count; i++)
            {
                var temp = users[i];
                users[i] = await webApiService.GetUserInfo(SteamUsers[i].SteamId64);
                users[i].AccountName = temp.AccountName;
                users[i].SteamID = temp.SteamID;
                users[i].PersonaName = temp.PersonaName;
                users[i].RememberPassword = temp.RememberPassword;
                users[i].MostRecent = temp.MostRecent;
                users[i].Timestamp = temp.Timestamp;
                users[i].LastLoginTime = temp.LastLoginTime;
                users[i].WantsOfflineMode = temp.WantsOfflineMode;
                users[i].SkipOfflineModeWarning = temp.SkipOfflineModeWarning;
                users[i].OriginVdfString = temp.OriginVdfString;
                users[i].AvatarStream = string.IsNullOrEmpty(users[i].AvatarFull) ? null : await httpService.GetImageAsync(users[i].AvatarFull, ImageChannelType.SteamAvatars);
            }
            SteamUsers = new ObservableCollection<SteamUser>(users.OrderByDescending(o => o.RememberPassword).ThenByDescending(o => o.LastLoginTime).ToList());
        }


        public void SteamId_Click(SteamUser user)
        {
            if (user.WantsOfflineMode)
            {
                UserModeChange(user, false);
            }
            steamService.SetCurrentUser(user.AccountName);
            steamService.TryKillSteamProcess();
            steamService.StartSteam(SteamSettings.SteamStratParameter.Value);
        }


        public void OfflineModeButton_Click(SteamUser user)
        {
            if (user.WantsOfflineMode == false)
            {
                UserModeChange(user, true);
            }
            SteamId_Click(user);
        }

        private void UserModeChange(SteamUser user, bool OfflineMode)
        {
            user.WantsOfflineMode = OfflineMode;
            steamService.UpdateLocalUserData(user);
            user.OriginVdfString = user.CurrentVdfString;
        }

        public void DeleteUserButton_Click(SteamUser user)
        {
            var result = MessageBoxCompat.ShowAsync("确定要删除这条本地记录帐户数据吗？" + Environment.NewLine + "这将会删除此账户在本地的Steam缓存数据。", ThisAssembly.AssemblyTrademark, MessageBoxButtonCompat.OKCancel).ContinueWith(s =>
            {
                if (s.Result == MessageBoxResultCompat.OK)
                {
                    steamService.DeleteLocalUserData(user);
                    SteamUsers.Remove(user);
                }
            });
        }
    }
}