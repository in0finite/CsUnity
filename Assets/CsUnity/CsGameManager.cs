using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uSource.Formats.Source.VBSP;
using System.IO;
using SourceUtils;

namespace CsUnity
{
    public class CsGameManager
    {
        public static event System.Action<ValveBspFile> OnMapLoaded = delegate { };


        static CsGameManager()
        {
            VBSPFile.OnLoaded -= OnBspLoaded;
            VBSPFile.OnLoaded += OnBspLoaded;
        }

        static void OnBspLoaded(Stream stream)
        {
            // initialize SourceUtils

            stream.Position = 0;

            using ValveBspFile bspFile = new ValveBspFile(((FileStream)stream).Name);
            
            OnMapLoaded(bspFile);

            bspFile.Dispose();
        }
    }
}
