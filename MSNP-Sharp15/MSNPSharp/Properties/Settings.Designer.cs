﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:2.0.50727.3053
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace MSNPSharp.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "8.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://ows.messenger.msn.com/OimWS/oim.asmx")]
        public string MSNPSharp_MSNWS_MSNOIMStoreService_OIMStoreService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNOIMStoreService_OIMStoreService"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://rsi.hotmail.com/rsi/rsi.asmx")]
        public string MSNPSharp_MSNWS_MSNRSIService_RSIService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNRSIService_RSIService"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://login.live.com/RST2.srf")]
        public string MSNPSharp_MSNWS_MSNSecurityTokenService_SecurityTokenService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNSecurityTokenService_SecurityTokenService"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://storage.msn.com/storageservice/SchematizedStore.asmx")]
        public string MSNPSharp_MSNWS_MSNStorageService_StorageService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNStorageService_StorageService"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://contacts.msn.com/abservice/SharingService.asmx")]
        public string MSNPSharp_MSNWS_MSNABSharingService_SharingService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNABSharingService_SharingService"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("http://cc.services.spaces.live.com/contactcard/contactcardservice.asmx")]
        public string MSNPSharp_MSNWS_MSNSpaceService_SpaceService {
            get {
                return ((string)(this["MSNPSharp_MSNWS_MSNSpaceService_SpaceService"]));
            }
        }
    }
}