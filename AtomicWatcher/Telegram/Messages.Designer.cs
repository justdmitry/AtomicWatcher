//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AtomicWatcher.Telegram {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AtomicWatcher.Telegram.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized for this operation..
        /// </summary>
        public static string AccessDenied {
            get {
                return ResourceManager.GetString("AccessDenied", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Available commands:
        ////help - this message
        ////inventory - list of NFTs you own
        ////settings - notification settings
        ////rules - notification rules.
        /// </summary>
        public static string Help {
            get {
                return ResourceManager.GetString("Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sorry, account `{0}` is not watched..
        /// </summary>
        public static string Inventory_AccountNotFound {
            get {
                return ResourceManager.GetString("Inventory_AccountNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Rule `{0}` was added.
        ///Send /rules to see full list..
        /// </summary>
        public static string Rule_Add_Ok {
            get {
                return ResourceManager.GetString("Rule_Add_Ok", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong arguments. Send /rules_help for help.
        /// </summary>
        public static string Rules_Add_WrongArguments {
            get {
                return ResourceManager.GetString("Rules_Add_WrongArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can&apos;t delete rule `{0}` - rule not found..
        /// </summary>
        public static string Rules_Delete_NotFound {
            get {
                return ResourceManager.GetString("Rules_Delete_NotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Rule `{0}` deleted.
        ///Send /rules to see actual list..
        /// </summary>
        public static string Rules_Delete_Ok {
            get {
                return ResourceManager.GetString("Rules_Delete_Ok", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong arguments. Correct syntax:
        ///`/rules del &lt;rule id&gt;`.
        /// </summary>
        public static string Rules_Delete_WrongArguments {
            get {
                return ResourceManager.GetString("Rules_Delete_WrongArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Manage your watch rules:
        ///`/rules` - list all your rules
        ///`/rules add &lt;param1&gt; &lt;param2&gt;...` - add new rule (see params below)
        ///`/rules del &lt;rule id&gt;` - delete rule #id
        ///`/rules help` - this message
        ///
        ///Allowed params for &quot;rules add&quot;:
        ///`min-mint`=&lt;X&gt;, `max-mint`=&lt;X&gt; - filter by mint
        ///`min-template`=&lt;X&gt;, `max-template`=&lt;X&gt; - filter by template Id
        ///`min-price`=&lt;X&gt;, `max-price`=&lt;X&gt; - filter by price (in WAX)
        ///`rarity`={ common | uncommon | rare | epic | legendary } - filter by rarity
        ///`absent` - notify when you  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Rules_Help {
            get {
                return ResourceManager.GetString("Rules_Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have no rules. Read /rules_help to create one..
        /// </summary>
        public static string Rules_None {
            get {
                return ResourceManager.GetString("Rules_None", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have enough rules, you can&apos;t add more..
        /// </summary>
        public static string Rules_TooMany {
            get {
                return ResourceManager.GetString("Rules_TooMany", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This command (settings) is deprecated. User /rules command instead..
        /// </summary>
        public static string Settings_Deprecated {
            get {
                return ResourceManager.GetString("Settings_Deprecated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Account {0} added to user {1}. Send /users to see full list..
        /// </summary>
        public static string Users_Add_Success {
            get {
                return ResourceManager.GetString("Users_Add_Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong arguments. Correct syntax:
        ///`/users add &lt;wax account&gt; &lt;telegram id&gt;`.
        /// </summary>
        public static string Users_Add_WrongArguments {
            get {
                return ResourceManager.GetString("Users_Add_WrongArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Account `{0}` is not found..
        /// </summary>
        public static string Users_Disable_NotFound {
            get {
                return ResourceManager.GetString("Users_Disable_NotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Account `{0}` deactivated..
        /// </summary>
        public static string Users_Disable_Success {
            get {
                return ResourceManager.GetString("Users_Disable_Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong arguments. Correct syntax:
        ///`/users disable &lt;wax account&gt;`.
        /// </summary>
        public static string Users_Disable_WrongArguments {
            get {
                return ResourceManager.GetString("Users_Disable_WrongArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Account `{0}` is not found..
        /// </summary>
        public static string Users_Enable_NotFound {
            get {
                return ResourceManager.GetString("Users_Enable_NotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Account `{0}` activated successfully..
        /// </summary>
        public static string Users_Enable_Success {
            get {
                return ResourceManager.GetString("Users_Enable_Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Wrong arguments. Correct syntax:
        ///`/users enable &lt;wax account&gt;`.
        /// </summary>
        public static string Users_Enable_WrongArguments {
            get {
                return ResourceManager.GetString("Users_Enable_WrongArguments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Manage registered users:
        ///`/users` - list all users
        ///`/users add &lt;wax account&gt; &lt;telegram id&gt;` - add new user or remap to different TelegramId
        ///`/users enable &lt;wax account&gt;` - enable user
        ///`/users disable &lt;wax account&gt;` - disable user
        ///`/users help` - this message.
        /// </summary>
        public static string Users_Help {
            get {
                return ResourceManager.GetString("Users_Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sorry, your account is disabled..
        /// </summary>
        public static string YouAreDisabled {
            get {
                return ResourceManager.GetString("YouAreDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sorry, you are not registered..
        /// </summary>
        public static string YouAreNotRegistered {
            get {
                return ResourceManager.GetString("YouAreNotRegistered", resourceCulture);
            }
        }
    }
}
