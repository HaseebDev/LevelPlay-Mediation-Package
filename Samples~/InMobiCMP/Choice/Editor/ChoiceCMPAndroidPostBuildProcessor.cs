using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

public class ChoiceCMPAndroidPostBuildProcessor : IPostGenerateGradleAndroidProject
{

    public int callbackOrder 
    {
        get {
            return 999;
        }
    }

    void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path) 
    {
        Debug.Log("Bulid path : " + path);
        
        string gradlePropertiesFile = path.Replace("unityLibrary", "") + "gradle.properties";

        string jvmArgsProperty = "org.gradle.jvmargs=-Xmx4096M";
        string androidXProperty = "android.useAndroidX=true";


        if (File.Exists(gradlePropertiesFile)) 
        {
            // Read the contents of the file
            string fileContent = File.ReadAllText(gradlePropertiesFile);

            // Check if the property is already present
            if (fileContent.Contains(androidXProperty))
            {
                // Property already exists
                Debug.Log("AndroidX Property already exists in the file.");
            }
            else
            {
                // Append the property to the file
                File.AppendAllText(gradlePropertiesFile, Environment.NewLine + androidXProperty + Environment.NewLine);
                Debug.Log("AndroidX Property appended to the file.");
            }
        }
        else 
        {
            // Create a new file and add both properties
            string newFileContent = androidXProperty + Environment.NewLine + jvmArgsProperty + Environment.NewLine;
            File.WriteAllText(gradlePropertiesFile, newFileContent);
            Debug.Log("File created, android.useAndroidX and org.gradle.jvmargs properties added.");
        }

    }
}
