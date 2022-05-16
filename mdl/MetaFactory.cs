using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE1006 // Type or member is obsolete

namespace mdl {

   
    
    /// <summary>
    /// Factory used to create registered instances of classes
    /// </summary>
    public interface IMetaFactory {
        /// <summary>
        /// Creates an instance of a class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T createInstance<T>() where T : class;

        /// <summary>
        /// Register a concrete type attaching it to an abstract type
        /// </summary>
        /// <param name="concreteType"></param>
        /// <param name="abstractType"></param>
        void registerType(Type concreteType, Type abstractType);


        /// <summary>
        /// Create an instance of a registered concrete type if T is a registered abstract type. Otherwise if T is a concrete type create an instance of T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T create<T>() where T : class;


        /// <summary>
        /// Creates a unique instance of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T getSingleton<T>() where T : class;

        /// <summary>
        /// Register an instance of a class as the singleton for a specified type.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="O"></param>
        void setSingleton(Type T, object O);
    }


    /// <summary>
    /// Factory used to create objects everywhere
    /// </summary>
    public class MetaFactory : IMetaFactory {
        private static IMetaFactory _factory = new MetaFactory();
        /// <summary>
        /// 
        /// </summary>
        public static IMetaFactory factory {
            get { return _factory; }
        } 

        static MetaFactory() {
            factory.registerType(typeof(MetaModel), typeof(IMetaModel));
            factory.registerType(typeof(GetData), typeof(IGetData));
            factory.registerType(typeof(PostData), typeof(PostData));
        }
        /// <summary>
        /// Creates an instance of a type using registered constructors
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T create<T>() where T : class {
            return factory.createInstance<T>();
        }

        T IMetaFactory.getSingleton<T>() {
            return MetaFactory.getSingleton<T>();
        }

        void IMetaFactory.setSingleton(Type T,object o) {
            var abstractName = T.Name;
            Singletons[abstractName] =  o;
        }

        T IMetaFactory.create<T>() {
            return createInstance<T>();
        }

        /// <summary>
        /// Creates an instance of a type using registered constructors
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T getSingleton<T>() where T : class {
            var abstractName = typeof(T).Name;
            if (Singletons.ContainsKey(abstractName))return  Singletons[abstractName] as T;
            Singletons[abstractName] =  factory.createInstance<T>();
            return  (T) Singletons[abstractName];
        }


        private static bool isConcreteType(Type type) {
            return type.IsClass && !type.IsAbstract && !type.IsInterface;
        }

        private readonly Dictionary<string, Func<object>> _lookup = new Dictionary<string, Func<object>>();
        private static readonly Dictionary<string, object> Singletons = new Dictionary<string, object>();

       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
       /// <exception cref="ArgumentException"></exception>
       public virtual T createInstance<T>() where T:class {
           var abstractName = typeof(T).Name;
           if (_lookup.ContainsKey(abstractName)) return _lookup[abstractName]() as T;
           if (isConcreteType(typeof(T))) {
               registerType(typeof(T), typeof(T));
           }
           else {
               throw new ArgumentException($"{abstractName} has not been registered", nameof(T));
           }
           return _lookup[abstractName]() as T;                
       }

        

        /// <summary>
        /// Register the concrete type associated with an abstract type
        /// </summary>
        /// <param name="concreteType"></param>
        /// <param name="abstractType"></param>
        public virtual void registerType(Type concreteType, Type abstractType) {
            _lookup[abstractType.Name] = createDefaultConstructor(concreteType);
        }

        private static Func<object> createDefaultConstructor(Type type) {
            var newExp = Expression.New(type);

            // Create a new lambda expression with the NewExpression as the body.
            var lambda = Expression.Lambda<Func<object>>(newExp);

            // Compile our new lambda expression.
            return lambda.Compile();
        }
    }

  


}
