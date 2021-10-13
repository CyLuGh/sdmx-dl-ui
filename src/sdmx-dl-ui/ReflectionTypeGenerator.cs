using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sdmx_dl_ui
{
    public static class ReflectionTypeGenerator
    {
        public static Type GenerateTypedObject( IEnumerable<string> elements )
            => GenerateTypedObject( elements , typeof( double? ) );

        public static Type GenerateTypedObject( IEnumerable<string> elements , Type propertiesType )
            => GenerateTypedObject( elements.Select( x => (x, propertiesType) ) );

        public static Type GenerateTypedObject( IEnumerable<(string name, Type propertyType)> elements , IEnumerable<(string name, Type parameterType)> constructorParameters = null , IEnumerable<Type> interfaces = null )
        {
            // https://msdn.microsoft.com/en-us/library/2sd82fz7.aspx
            var domain = Thread.GetDomain();
            var myAsmName = new AssemblyName { Name = "RuntimeDynamic" };

            var myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly( myAsmName , AssemblyBuilderAccess.Run );
            var moduleBuilder = myAsmBuilder.DefineDynamicModule( myAsmName.Name );

            var typeBuilder = moduleBuilder.DefineType( "GenType" , TypeAttributes.Public );

            var constructorFields = new List<FieldBuilder>();

            ConstructorBuilder constructorBuilder;
            if ( constructorParameters != null )
            {
                var parameters = constructorParameters.ToArray();
                constructorBuilder = typeBuilder.DefineConstructor( MethodAttributes.Public ,
                    CallingConventions.Standard ,
                    parameters.Select( x => x.parameterType ).ToArray() );

                for ( int i = 0 ; i < parameters.Length ; i++ )
                {
                    var parameter = parameters[i];
                    var parameterBuilder = constructorBuilder.DefineParameter( i + 1 , ParameterAttributes.None , parameter.name );

                    // Create underlying field; all properties have a field of the same type
                    FieldBuilder field =
                        typeBuilder.DefineField( $"_{parameter.name}" , parameter.parameterType , FieldAttributes.Private | FieldAttributes.InitOnly );
                    constructorFields.Add( field );
                }
            }
            else
            {
                constructorBuilder = typeBuilder.DefineConstructor( MethodAttributes.Public , CallingConventions.Standard , Type.EmptyTypes );
            }

            foreach ( var (name, propertyType) in elements )
            {
                var fieldBuilder = typeBuilder.DefineField( $"_{name}" , propertyType , FieldAttributes.Private );

                var propBuilder = typeBuilder.DefineProperty( name , PropertyAttributes.HasDefault , propertyType , null );
                var getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                // Define the "get" accessor method
                var custGetPropMthdBldr = typeBuilder.DefineMethod( $"get_{name}" , getSetAttr , propertyType , Type.EmptyTypes );
                var custGetIL = custGetPropMthdBldr.GetILGenerator();
                custGetIL.Emit( OpCodes.Ldarg_0 );
                custGetIL.Emit( OpCodes.Ldfld , fieldBuilder );
                custGetIL.Emit( OpCodes.Ret );

                // Define the "set" accessor method
                var custSetPropMthdBldr = typeBuilder.DefineMethod( $"set_{name}" , getSetAttr , null , new Type[] { propertyType } );
                var custSetIL = custSetPropMthdBldr.GetILGenerator();
                custSetIL.Emit( OpCodes.Ldarg_0 );
                custSetIL.Emit( OpCodes.Ldarg_1 );
                custSetIL.Emit( OpCodes.Stfld , fieldBuilder );
                custSetIL.Emit( OpCodes.Ret );

                // Last, we must map the two methods created above to our PropertyBuilder to
                // their corresponding behaviors, "get" and "set" respectively.
                propBuilder.SetGetMethod( custGetPropMthdBldr );
                propBuilder.SetSetMethod( custSetPropMthdBldr );
            }

            // Add interfaces : https://www.codeproject.com/Articles/22832/Automatic-Interface-Implementer-An-Example-of-Runt
            interfaces?.ForEach( x => typeBuilder.AddInterfaceImplementation( x ) );

            var baseConstructorInfo = typeof( object ).GetConstructor( Array.Empty<Type>() );

            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit( OpCodes.Ldarg_0 );                      // Load "this"
            ilGenerator.Emit( OpCodes.Call , baseConstructorInfo );   // Call the base constructor

            if ( constructorParameters != null )
            {
                var parameters = constructorParameters.ToArray();
                for ( int i = 0 ; i < parameters.Length ; i++ )
                {
                    ilGenerator.Emit( OpCodes.Ldarg_0 ); //this
                    ilGenerator.Emit( OpCodes.Ldarg_S , i + 1 ); // parameter n
                    ilGenerator.Emit( OpCodes.Stfld , constructorFields[i] ); // set value in field
                }
            }

            ilGenerator.Emit( OpCodes.Ret );                          // return

            interfaces?.ForEach( x =>
            {
                var methods = new List<MethodInfo>();
                AddMethodsToList( methods , x );

                var properties = new List<PropertyInfo>();
                AddPropertiesToList( properties , x );

                foreach ( PropertyInfo pi in properties )
                {
                    string piName = pi.Name;
                    Type propertyType = pi.PropertyType;

                    // Create underlying field; all properties have a field of the same type
                    FieldBuilder field =
                        constructorFields.Find( o => o.Name.Equals( $"_{piName}" ) )
                        ??
                        typeBuilder.DefineField( "_" + piName , propertyType , FieldAttributes.Private );

                    var propBuilder = typeBuilder.DefineProperty( piName , PropertyAttributes.HasDefault , propertyType , null );

                    if ( pi.CanRead )
                    {
                        // If there is a getter in the interface, create a getter in the new type
                        MethodInfo getMethod = pi.GetGetMethod();
                        if ( getMethod != null )
                        {
                            // This will prevent us from creating a default method for the property's getter
                            methods.Remove( getMethod );

                            // Now we will generate the getter method
                            MethodBuilder methodBuilder = typeBuilder.DefineMethod( getMethod.Name ,
                                MethodAttributes.Public | MethodAttributes.Virtual , propertyType ,
                                Type.EmptyTypes );

                            // The ILGenerator class is used to put op-codes (similar to assembly)
                            // into the method
                            ilGenerator = methodBuilder.GetILGenerator();

                            // These are the op-codes, (similar to assembly)
                            ilGenerator.Emit( OpCodes.Ldarg_0 );      // Load "this"
                                                                      // Load the property's underlying field onto the stack
                            ilGenerator.Emit( OpCodes.Ldfld , field );
                            ilGenerator.Emit( OpCodes.Ret );          // Return the value on the stack

                            // We need to associate our new type's method with the
                            // getter method in the interface
                            typeBuilder.DefineMethodOverride( methodBuilder , getMethod );
                            propBuilder.SetGetMethod( methodBuilder );
                        }
                    }

                    if ( pi.CanWrite )
                    {
                        // If there is a setter in the interface, create a setter in the new type
                        MethodInfo setMethod = pi.GetSetMethod();
                        if ( setMethod != null )
                        {
                            // This will prevent us from creating a default method for the property's setter
                            methods.Remove( setMethod );

                            // Now we will generate the setter method
                            MethodBuilder methodBuilder = typeBuilder.DefineMethod
                                ( setMethod.Name , MethodAttributes.Public |
                                MethodAttributes.Virtual , typeof( void ) , new Type[] { pi.PropertyType } );

                            // The ILGenerator class is used to put op-codes (similar to assembly)
                            // into the method
                            ilGenerator = methodBuilder.GetILGenerator();

                            // These are the op-codes, (similar to assembly)
                            ilGenerator.Emit( OpCodes.Ldarg_0 );      // Load "this"
                            ilGenerator.Emit( OpCodes.Ldarg_1 );      // Load "value" onto the stack
                                                                      // Set the field equal to the "value" on the stack
                            ilGenerator.Emit( OpCodes.Stfld , field );
                            ilGenerator.Emit( OpCodes.Ret );          // Return nothing

                            // We need to associate our new type's method with the
                            // setter method in the interface
                            typeBuilder.DefineMethodOverride( methodBuilder , setMethod );
                            propBuilder.SetSetMethod( methodBuilder );
                        }
                    }
                }

                foreach ( MethodInfo methodInfo in methods )
                {
                    // Get the return type and argument types

                    Type returnType = methodInfo.ReturnType;

                    List<Type> argumentTypes = new List<Type>();
                    foreach ( ParameterInfo parameterInfo in methodInfo.GetParameters() )
                        argumentTypes.Add( parameterInfo.ParameterType );

                    // Define the method
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod
                    ( methodInfo.Name , MethodAttributes.Public |
                                        MethodAttributes.Virtual , returnType , argumentTypes.ToArray() );

                    // The ILGenerator class is used to put op-codes
                    // (similar to assembly) into the method
                    ilGenerator = methodBuilder.GetILGenerator();

                    // If there's a return type, create a default value or null to return
                    if ( returnType != typeof( void ) )
                    {
                        // this declares the local object, int, long, float, etc.
                        LocalBuilder localBuilder = ilGenerator.DeclareLocal( returnType );
                        // load the value on the stack to return
                        ilGenerator.Emit( OpCodes.Ldloc , localBuilder );
                    }

                    ilGenerator.Emit( OpCodes.Ret );                       // return

                    // We need to associate our new type's method with the method in the interface
                    typeBuilder.DefineMethodOverride( methodBuilder , methodInfo );
                }
            } );

            return typeBuilder.CreateType();
        }

        private static void AddMethodsToList( List<MethodInfo> methods , Type type )
        {
            methods.AddRange( type.GetMethods() );

            foreach ( var subInterface in type.GetInterfaces() )
                AddMethodsToList( methods , subInterface );
        }

        private static void AddPropertiesToList( List<PropertyInfo> properties , Type type )
        {
            properties.AddRange( type.GetProperties() );

            foreach ( var subInterface in type.GetInterfaces() )
                AddPropertiesToList( properties , subInterface );
        }

        public static string ToHeaderCopyString<T>( this T instance ) where T : class
        {
            if ( instance == null )
                return string.Empty;

            return string.Join( "\t" , instance.GetType().GetProperties().Select( x => x.Name ) );
        }

        public static string ToHeaderCopyString( this Type t )
            => string.Join( "\t" , t.GetProperties().Select( x => x.Name ) );

        public static string ToHeaderCopyString( this Type t , params string[] properties )
            => string.Join( "\t" , t.GetProperties().Select( x => x.Name ).Where( properties.Contains ) );

        public static string ToCopyString<T>( this T instance ) where T : class
        {
            if ( instance == null )
                return string.Empty;

            return string.Join( "\t" , instance.GetType().GetProperties().Select( x => x.GetValue( instance , null ) ?? string.Empty ) );
        }

        public static string ToCopyString<T>( this T instance , params string[] properties ) where T : class
        {
            if ( instance == null )
                return string.Empty;

            return string.Join( "\t" , instance.GetType().GetProperties().Where( x => properties.Contains( x.Name ) ).Select( x => x.GetValue( instance , null ) ?? string.Empty ) );
        }
    }
}