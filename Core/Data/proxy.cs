using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Uniya.Core;

/// <summary>The proxy for all IDB interfaces.</summary>
public static class XProxy
{
    // ------------------------------------------------------------------------------------
    #region ** create instance for IDB interface

    /// <summary>
    /// Create object instance by interface.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <returns>The object instance.</returns>
    public static T Get<T>()
    {
        var assemblyName = Guid.NewGuid().ToString();
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Module");
        var name = typeof(T).Name.Substring(1);     // remove first interface symbol 'I'
        var type = module.DefineType(name, TypeAttributes.Public, typeof(object));
        var fieldsList = new List<string>();
        type.AddInterfaceImplementation(typeof(T));
        foreach (var v in typeof(T).GetPublicProperties())
        {
            fieldsList.Add(v.Name);
            var field = type.DefineField("_" + v.Name.ToLower(), v.PropertyType, FieldAttributes.Private);
            var property = type.DefineProperty(v.Name, PropertyAttributes.None, v.PropertyType, new Type[0]);
            var getter = type.DefineMethod("get_" + v.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, v.PropertyType, new Type[0]);
            var setter = type.DefineMethod("set_" + v.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, null, new Type[] { v.PropertyType });
            var getGenerator = getter.GetILGenerator();
            var setGenerator = setter.GetILGenerator();
            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Ldfld, field);
            getGenerator.Emit(OpCodes.Ret);
            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            setGenerator.Emit(OpCodes.Stfld, field);
            setGenerator.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
            property.SetSetMethod(setter);
            type.DefineMethodOverride(getter, v.GetGetMethod());
            type.DefineMethodOverride(setter, v.GetSetMethod());
        }
        return (T)Activator.CreateInstance(type.CreateType());
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** getter/setter methods (using names)

    /// <summary>
    /// Gets value by public field or property name.
    /// </summary>
    /// <param name="obj">The object with field or property name.</param>
    /// <param name="name">The name of field or property.</param>
    /// <returns>The value of the field or property, otherwise <b>null</b>.</returns>
    public static object GetValue(object obj, string name)
    {
        object value = null;
        Type type = obj.GetType();
        try
        {
            PropertyInfo pi = type.GetProperty(name);
            if (pi != null)
            {
                MethodInfo mi = pi.GetGetMethod();
                if (mi != null)
                {
#if FAST
                    value = GetValue(mi, obj);
#else
                    value = mi.Invoke(obj, null);
#endif
                }
            }
            else
            {
                FieldInfo fi = type.GetField(name);
                if (fi != null)
                {
#if FAST
                    value = GetValue(fi, obj);
#else
                    value = fi.GetValue(obj);
#endif
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.Message);
            value = null;
        }
        if (value != null && type.IsEnum)
        {
            value = (int)value;
        }
        return value;
    }
    /// <summary>
    /// Gets value by public field or property name.
    /// </summary>
    /// <param name="pi">The property information.</param>
    /// <param name="obj">The object with field or property name.</param>
    /// <returns>The value of the field or property, otherwise <b>null</b>.</returns>
    public static object GetValue(PropertyInfo pi, object obj)
    {
        object value = null;
        Type type = obj.GetType();
        try
        {
            MethodInfo mi = pi.GetGetMethod();
            if (mi != null)
            {
#if FAST
                value = GetValue(mi, obj);
#else
                value = mi.Invoke(obj, null);
#endif
            }
        }
        catch (Exception ex)
        {
            Console.Write(ex);
            value = null;
        }
        if (value != null && type.IsEnum)
        {
            value = (int)value;
        }
        return value;
    }
    /// <summary>
    /// Set value of property or field by name for object.
    /// </summary>
    /// <param name="obj">The object value.</param>
    /// <param name="name">The property or field name.</param>
    /// <param name="value">The new value of property or field.</param>
    public static void SetValue(object obj, string name, object value)
    {
        Type type = obj.GetType();
        PropertyInfo pi = type.GetProperty(name);
        if (pi != null)
        {
            MethodInfo mi = pi.GetSetMethod();
            if (mi != null)
            {
#if FAST
                SetValue(mi, obj, value);
#else
                mi.Invoke(obj, new object[] { value });
#endif
            }
        }
        else
        {
            FieldInfo fi = type.GetField(name);
            if (fi != null)
            {
#if FAST
                SetValue(fi, obj, value);
#else
                fi.SetValue(obj, value);
#endif
            }
            else
            {
                throw new ArgumentException("Unknown property or field name", "name");
            }
        }
    }
    /// <summary>
    /// Set value of property or field by name for object.
    /// </summary>
    /// <param name="pi">The property information.</param>
    /// <param name="obj">The object value.</param>
    /// <param name="value">The new value of property or field.</param>
    public static void SetValue(PropertyInfo pi, object obj, object value)
    {
        Type type = obj.GetType();
        MethodInfo mi = pi.GetSetMethod();
        if (mi != null && !(value is DBNull))
        {
            var pt = pi.PropertyType;
            if (pt.IsEnum)
            {
                foreach (var eval in pt.GetEnumValues())
                {
                    if (Convert.ToInt64(eval) == Convert.ToInt64(value))
                    {
                        mi.Invoke(obj, new object[] { eval });
                        break;
                    }
                }
            }
            else if (pt == typeof(DateTime))
            {
                mi.Invoke(obj, new object[] { Convert.ToDateTime(value.ToString()) });
            }
            else if (pt == typeof(bool))
            {
                mi.Invoke(obj, new object[] { Convert.ToBoolean(value) });
            }
            else
            {
                mi.Invoke(obj, new object[] { value });
            }
        }
    }

    /// <summary>
    /// Gets type of a property or a field with name.
    /// </summary>
    /// <param name="obj">The object value.</param>
    /// <param name="name">The property or field name.</param>
    /// <returns>The type of a property or a field.</returns>
    public static Type GetType(object obj, string name)
    {
        Type type = obj.GetType();
        PropertyInfo pi = type.GetProperty(name);
        if (pi != null)
        {
            MethodInfo mi = pi.GetSetMethod();
            if (mi != null)
                return pi.PropertyType;
        }
        else
        {
            FieldInfo fi = type.GetField(name);
            if (fi != null)
                return fi.FieldType;
        }
        return null;
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** extensions

    internal static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
    {
        if (!type.IsInterface)
            return type.GetProperties();

        return (new Type[] { type })
               .Concat(type.GetInterfaces())
               .SelectMany(i => i.GetProperties());
    }

    /// <summary>
    /// Delete file using <see cref="FileInfo"/>.
    /// </summary>
    /// <param name="fi"></param>
    /// <returns></returns>
    public static Task DeleteAsync(this FileInfo fi)
    {
        return Task.Factory.StartNew(() => fi.Delete());
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** embedded resource support

    /// <summary>
    /// Gets resource data as stream.
    /// </summary>
    /// <param name="name">The embedded resource name.</param>
    /// <returns>The stream that is need disposed after using.</returns>
    public static Stream GetResource(Assembly assembly, string name)
    {
        foreach (string res in assembly.GetManifestResourceNames())
        {
            if (res.EndsWith(name))
                return assembly.GetManifestResourceStream(res);
        }
        return null;
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** static password hash and salt

    /// <summary>
    /// Test password hash and salt.
    /// </summary>
    /// <param name="password">The text password.</param>
    /// <param name="hash">Output password hash.</param>
    /// <param name="salt">Output password salt.</param>
    /// <returns><b>true</b> if password verified, otherwise <b>false</b>.</returns>
    public static bool TestPassword(string password, out string hash, out string salt)
    {
        // create hash and salt of the password
        using (var hmac = new HMACSHA512())
        {
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        // done
        return VerifyPassword(password, hash, salt);
    }
    /// <summary>
    /// Verify password with hash and salt.
    /// </summary>
    /// <param name="password">The probably text password.</param>
    /// <param name="hash">The password hash.</param>
    /// <param name="salt">The password salt.</param>
    /// <returns><b>true</b> if password verified, otherwise <b>false</b>.</returns>
    public static bool VerifyPassword(string password, string hash, string salt)
    {
        // verify regular expression
        var hasNumber = new Regex(@"[0-9]+");
        var hasUpperChar = new Regex(@"[A-Z]+");
        var hasMinimum8Chars = new Regex(@".{8,}");

        // verify password
        if (!hasNumber.IsMatch(password) || !hasUpperChar.IsMatch(password) || !hasMinimum8Chars.IsMatch(password))
        {
            // bad done
            return false;
        }

        // verify hash and salt of the password
        using (var hmac = new HMACSHA512(Convert.FromBase64String(salt)))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(Convert.FromBase64String(hash));
        }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** static encrypt and decrypt model

    /// <summary>
    /// Encrypt text string using default password and default crypt algorithm.
    /// </summary>
    /// <param name="text">The text string.</param>
    /// <returns>Gets encrypted Base 64 text string.</returns>
    public static string Encrypt(string text)
    {
        return Encrypt(text, DefaultPassword, DefaultAlgorithm);
    }
    /// <summary>
    /// Encrypt text string using default crypt algorithm.
    /// </summary>
    /// <param name="text">The text string.</param>
    /// <param name="password">The text password.</param>
    /// <returns>Gets encrypted Base 64 text string.</returns>
    public static string Encrypt(string text, string password)
    {
        return Encrypt(text, password, DefaultAlgorithm);
    }
    /// <summary>
    /// Encrypt text string using default crypt algorithm.
    /// </summary>
    /// <param name="text">The text string.</param>
    /// <param name="password">The text password.</param>
    /// <param name="sa">The symmetric algorithm object.</param>
    /// <returns>Gets encrypted Base 64 text string.</returns>
    public static string Encrypt(string text, string password, SymmetricAlgorithm sa)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(text), password, sa));
    }
    /// <summary>
    /// Encrypt data buffer using default password and default crypt algorithm.
    /// </summary>
    /// <param name="data">The data buffer.</param>
    /// <returns>Gets encrypted data buffer.</returns>
    public static byte[] Encrypt(byte[] data)
    {
        return Encrypt(data, DefaultPassword, DefaultAlgorithm);
    }
    /// <summary>
    /// Encrypt data buffer using default crypt algorithm.
    /// </summary>
    /// <param name="data">The data buffer.</param>
    /// <param name="password">The text password.</param>
    /// <returns>Gets encrypted data buffer.</returns>
    public static byte[] Encrypt(byte[] data, string password)
    {
        return Encrypt(data, password, DefaultAlgorithm);
    }
    /// <summary>
    /// Encrypt data buffer.
    /// </summary>
    /// <param name="data">The data buffer.</param>
    /// <param name="password">The text password.</param>
    /// <param name="sa">The symmetric algorithm object.</param>
    /// <returns>Gets encrypted data buffer.</returns>
    public static byte[] Encrypt(byte[] data, string password, SymmetricAlgorithm sa)
    {
        using (var ct = sa.CreateEncryptor((new PasswordDeriveBytes(password, null)).GetBytes(16), new byte[16]))
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Decrypt data buffer using default password and default crypt algorithm.
    /// </summary>
    /// <param name="text">The encrypted Base 64 text string.</param>
    /// <returns>Gets decrypted text string.</returns>
    public static string Decrypt(string text)
    {
        return Decrypt(text, DefaultPassword, DefaultAlgorithm);
    }
    /// <summary>
    /// Decrypt data buffer using default crypt algorithm.
    /// </summary>
    /// <param name="text">The encrypted Base 64 text string.</param>
    /// <param name="password">The text password.</param>
    /// <returns>Gets decrypted text string.</returns>
    public static string Decrypt(string text, string password)
    {
        return Decrypt(text, password, DefaultAlgorithm);
    }
    /// <summary>
    /// Decrypt data buffer.
    /// </summary>
    /// <param name="text">The encrypted Base 64 text string.</param>
    /// <param name="password">The text password.</param>
    /// <param name="sa">The symmetric algorithm object.</param>
    /// <returns>Gets decrypted text string.</returns>
    public static string Decrypt(string text, string password, SymmetricAlgorithm sa)
    {
        // sanity
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // decrypt
        try
        {
            using (var ct = sa.CreateDecryptor((new PasswordDeriveBytes(password, null)).GetBytes(16), new byte[16]))
            using (var ms = new MemoryStream(Convert.FromBase64String(text)))
            using (var cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs, Encoding.UTF8))
            {
                // done
                return sr.ReadToEnd();
            }
        }
        catch
        {
            // bad done
            return text;
        }
    }
    /// <summary>
    /// Decrypt data buffer using default password and default crypt algorithm.
    /// </summary>
    /// <param name="data">The encrypted data buffer.</param>
    /// <returns>Gets decrypted data buffer.</returns>
    public static byte[] Decrypt(byte[] data)
    {
        return Decrypt(data, DefaultPassword, DefaultAlgorithm);
    }
    /// <summary>
    /// Decrypt data buffer using default password and default crypt algorithm.
    /// </summary>
    /// <param name="data">The encrypted data buffer.</param>
    /// <param name="password">The text password.</param>
    /// <returns>Gets decrypted data buffer.</returns>
    public static byte[] Decrypt(byte[] data, string password)
    {
        return Decrypt(data, password, DefaultAlgorithm);
    }
    /// <summary>
    /// Decrypt data buffer using default password and default crypt algorithm.
    /// </summary>
    /// <param name="data">The encrypted data buffer.</param>
    /// <param name="password">The text password.</param>
    /// <param name="sa">The symmetric algorithm object.</param>
    /// <returns>Gets decrypted data buffer.</returns>
    public static byte[] Decrypt(byte[] data, string password, SymmetricAlgorithm sa)
    {
        try
        {
            using (var ct = sa.CreateDecryptor((new PasswordDeriveBytes(password, null)).GetBytes(16), new byte[16]))
            using (var ms = new MemoryStream(data))
            using (var cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
            using (var br = new BinaryReader(cs))
            {
                return br.ReadBytes((int)cs.Length);
            }
        }
        catch
        {
            return data;
        }
    }
    private static string DefaultPassword
    {
        get { return Assembly.GetExecutingAssembly().FullName.Split(' ')[0].ToUpperInvariant(); }
    }
    private static SymmetricAlgorithm DefaultAlgorithm
    {
        get { return Aes.Create("AesManaged"); }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** gets object from text

    public static object GetValue(string text)
    {
        // is option set?
        if (XOptionSetValue.TryParse(text, out XOptionSetValue optionSet))
        {
            return optionSet;
        }

        // is reference?
        if (XEntityReference.TryParse(text, out XEntityReference entityRef))
        {
            return entityRef;
        }

        // is integer?
        if (int.TryParse(text, out int n))
        {
            if (text[0] == '0')
                return text;
            return n;
        }

        // is long integer?
        if (long.TryParse(text, out long l))
        {
            if (text[0] == '0')
                return text;
            return l;
        }

        // is GUID?
        if (Guid.TryParse(text, out Guid guid))
        {
            return guid;
        }

        // is boolean?
        if (bool.TryParse(text, out bool b) && text.Equals(new XElement("n", b).Value))
        {
            return b;
        }

        // set of format providers
        var cultures = new List<IFormatProvider>
        {
            CultureInfo.CurrentUICulture,
            CultureInfo.CurrentCulture,
            CultureInfo.InstalledUICulture,
            CultureInfo.InvariantCulture
        };

        // is numeric?
        foreach (var culture in cultures)
        {
            if (!IsNumber(text)) break;
            if (double.TryParse(text, NumberStyles.Float, culture, out double d) && text.Equals(new XElement("n", d).Value))
                return d;
            if (double.TryParse(text, NumberStyles.Any, culture, out d) && text.Equals(new XElement("n", d).Value))
                return d;
        }

        // is date & time?
        if (IsUtcDate(text))
        {
            var parts = text.Split('T');
            var ss = parts[0].Split('-');
            if (ss.Length >= 3 && IsNumber(ss[0]) && IsNumber(ss[1]) && IsNumber(ss[2]))
            {
                int year = int.Parse(ss[0]);
                int month = int.Parse(ss[1]);
                int day = int.Parse(ss[2]);

                int hour = 0, minute = 0, second = 0;
                if (parts.Length > 1)
                {
                    ss = parts[1].Split(':', '+');
                    if (IsNumber(ss[0])) hour = int.Parse(ss[0]);
                    if (ss.Length > 1 && IsNumber(ss[1])) minute = int.Parse(ss[1].Trim(' ', '$'));
                    if (ss.Length > 2 && IsNumber(ss[2])) second = int.Parse(ss[2].Trim(' ', '$'));
                }

                return new DateTime(year, month, day, hour, minute, second);
            }
        }
        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out DateTime dt))
                return dt;
        }

        // done
        return text;
    }

    internal static bool IsNumber(string text)
    {
        if (text.Length > 0 && text.Length <= 32)
        {
            var regex = new Regex(@"^-*[0-9,\.]+$");
            if (regex.IsMatch(text))
                return true;
            regex = new Regex(@"^(-?[1-9]+\\d*([.]\\d+)?)$|^(-?0[.]\\d*[1-9]+)$|^0$");
            if (regex.IsMatch(text))
                return true;
            regex = new Regex(@"^(-?[1-9]+\\d*([,]\\d+)?)$|^(-?0[,]\\d*[1-9]+)$|^0$");
            if (regex.IsMatch(text))
                return true;
        }
        return false;
    }
    internal static bool IsUtcDate(string text)
    {
        if (text.Length > 4 && text.Length <= 32)
        {
            var regex = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}$");
            return regex.IsMatch(text.Split('+')[0]);
        }
        return false;
    }

    #endregion
}
