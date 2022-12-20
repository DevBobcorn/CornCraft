using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy {
    [InitializeOnLoad]
    public class E_AddCozyModules : Editor
    {


        public E_AddCozyModules()
        {


            List<Type> listOfMods = (
          from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
          from type in domainAssembly.GetTypes()
          where typeof(CozyModule).IsAssignableFrom(type) && type != typeof(CozyModule)
          select type).ToList();

            listOfMods.Remove(typeof(CozyModule));


        }
    }
}