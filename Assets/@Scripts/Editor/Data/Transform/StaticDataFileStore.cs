using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using F1.Data;

namespace F1.Editor.Data
{
    /// <summary>
    /// File access for the transform, shared by the Unity menu and Tools/Sim. Generated files are
    /// replaced only after the whole transform succeeded, and only when their content changed.
    /// </summary>
    public sealed class StaticDataFileStore
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        readonly string _sourceDirectory;
        readonly string _generatedDirectory;

        /// <param name="projectRoot">The Unity project root (the directory that contains "Assets").</param>
        public StaticDataFileStore(string projectRoot)
        {
            _sourceDirectory = Path.Combine(projectRoot, StaticDataFiles.SourceDirectory);
            _generatedDirectory = Path.Combine(projectRoot, StaticDataFiles.GeneratedDirectory);
        }

        /// <summary>CSV text of a source file, or null when it does not exist.</summary>
        public string ReadSource(string fileName)
        {
            string path = Path.Combine(_sourceDirectory, fileName);
            return File.Exists(path) ? File.ReadAllText(path, Utf8) : null;
        }

        /// <summary>JSON text of a generated file, or null when it does not exist.</summary>
        public string ReadGenerated(string fileName)
        {
            string path = Path.Combine(_generatedDirectory, fileName);
            return File.Exists(path) ? File.ReadAllText(path, Utf8) : null;
        }

        /// <summary>Generated files whose content differs from a fresh transform of the sources.</summary>
        public List<string> FindStale(TransformResult result)
        {
            var stale = new List<string>();
            foreach (KeyValuePair<string, string> file in result.GeneratedFiles)
            {
                if (ReadGenerated(file.Key) != file.Value)
                {
                    stale.Add(file.Key);
                }
            }

            stale.Sort(StringComparer.Ordinal);
            return stale;
        }

        /// <summary>Writes the changed files and returns their names. Unchanged files are not touched.</summary>
        public List<string> WriteGenerated(TransformResult result)
        {
            List<string> changed = FindStale(result);
            if (changed.Count == 0)
            {
                return changed;
            }

            Directory.CreateDirectory(_generatedDirectory);

            // Write everything to temp files first so a failure cannot leave a half-updated set.
            foreach (string fileName in changed)
            {
                File.WriteAllText(Path.Combine(_generatedDirectory, fileName + ".tmp"), result.GeneratedFiles[fileName], Utf8);
            }

            foreach (string fileName in changed)
            {
                string path = Path.Combine(_generatedDirectory, fileName);
                string temp = path + ".tmp";
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }

            return changed;
        }
    }
}
