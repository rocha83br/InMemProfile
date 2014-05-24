using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Linq;

namespace System.Security.InMemProfile
{
	public class AccessValidator
    {
        #region Declarations

        public const int ProfileKeySize = 1024; // 1024 bits
        public static int ConnectedUsers;

        #endregion

        #region Public Methods

        public static bool ValidatePassword(string cryptoPwd, string pwd)
        {
            Encrypter cripto = new Encrypter();

            return cryptoPwd.Equals(cripto.EncryptText(pwd));
        }

        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> ListFuncionalities(string domainAssemblyPath, string controllerAssemblyPath, string profileKey)
        {
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> result =
                            new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            Assembly controllerAssembly;
            IEnumerable<Type> systemEntities = GetSystemEntities(domainAssemblyPath, out controllerAssembly);

            foreach (Type entity in systemEntities)
            {
                var ctrlAssemblyInstance = Assembly.LoadFrom(controllerAssemblyPath);

                if (CheckPermission(entity.Name, ctrlAssemblyInstance, profileKey))
                {
                    var entityFuncionality = entity.GetCustomAttributes(true).
                                                    Where(ant => ant.GetType().
                                                    Name.Equals("Funcionality")).
                                                    FirstOrDefault();

                    string funcionalityGroup = entityFuncionality.GetType().GetField("FuncionalityGroup").
                                                                  GetValue(entityFuncionality).ToString();

                    string funcionalitySubGroup = entityFuncionality.GetType().GetField("FuncionalitySubGroup").
                                                                     GetValue(entityFuncionality).ToString();

                    List<string> attributeDescriptions = new List<string>();

                    if (!result.Keys.Any(key => key.Equals(funcionalityGroup)))
                        result.Add(funcionalityGroup, new Dictionary<string, Dictionary<string, string>>());

                    if (!result[funcionalityGroup].Any(sbg => sbg.Key.Equals(funcionalitySubGroup)))
                        result[funcionalityGroup].Add(funcionalitySubGroup, new Dictionary<string, string>());

                    var subGroups = result[funcionalityGroup];

                    var entityDisplayName = entity.GetCustomAttributes(true).
                                                   Where(ant => ant.GetType().
                                                   Name.Equals("DisplayNameAttribute")).
                                                   FirstOrDefault();

                    string displayName = entityDisplayName.GetType().GetProperty("DisplayName").
                                                           GetValue(entityDisplayName, null).ToString();

                    if (!subGroups.Keys.Any(key => key.Equals(funcionalitySubGroup)))
                        subGroups.Add(funcionalitySubGroup, new Dictionary<string, string>());

                    var funcionalityAccess = entityFuncionality.
                                         GetType().GetField("FuncionalityAccess").
                                         GetValue(entityFuncionality).ToString();

                    subGroups[funcionalitySubGroup].Add(displayName, funcionalityAccess);
                 }
            }

            return result;
        }

        public static bool CheckPermission(string entityName, Assembly controllerAssembly, string profileKey)
        {
            return checkPermission(controllerAssembly, entityName, profileKey);
        }

        public static bool CheckPermission(int funcPosition, string profileKey)
        {
            return getBinaryProfileKey(profileKey)[funcPosition];
        }

        public static IEnumerable<Type> GetSystemEntities(string assemblyPath, out Assembly entitiesLib)
        {
            string libsPath = ConfigurationManager.AppSettings["BinPath"];

            entitiesLib = Assembly.LoadFrom(string.Concat(libsPath, assemblyPath));

            IEnumerable<Type> entities = entitiesLib.GetTypes()
                                         .Where(et => et.GetCustomAttributes(true)
                                         .Any(ant => ant.GetType().Name.Equals("Funcionality")));

            return entities;
        }

        #endregion

        #region Helper Methods

        internal static bool[] getBinaryProfileKey(string profileKey)
        {
            BitArray arrayDecryptedKey = null;
            byte[] preDecryptedKey = new byte[AccessValidator.ProfileKeySize];
            bool[] decryptedKey = new bool[AccessValidator.ProfileKeySize];

            Encrypter cripto = new Encrypter();

            preDecryptedKey = cripto.DecryptText(ref profileKey);

            arrayDecryptedKey = new BitArray(preDecryptedKey);
            arrayDecryptedKey.Length = AccessValidator.ProfileKeySize;

            arrayDecryptedKey.CopyTo(decryptedKey, 0);

            return decryptedKey;
        }

        internal static bool checkPermission(Assembly controllerAssembly, string entityTypeName, string profileKey)
        {
            bool[] decryptedProfileKey = getBinaryProfileKey(profileKey);

            Type profileType = null;
            foreach (var fndType in controllerAssembly.GetTypes())
            {
                if (fndType.Name.Equals("EntityAccessProfile"))
                {
                    profileType = fndType;
                    break;
                }
            }

            var accessControl =  Activator.CreateInstance(profileType);

            int profileCode = int.Parse(accessControl.GetType().GetField(entityTypeName)
                                        .GetValue(accessControl).ToString());

            return decryptedProfileKey[profileCode];
        }

        #endregion
    }
}
