using System.Collections.Generic;
using UnityEngine;
using System.IO; 

// class for single question
[System.Serializable]
public class QuizQuestion
{
    public string question;
    public string[] options;
    public string answer;
}

// class for quiz ds
[System.Serializable]
public class QuizData
{
    public Dictionary<string, List<QuizQuestion>> questions;
}

public class QuizDataLoader : MonoBehaviour
{
    public QuizData quizData;  // stores loaded quiz data

    void Start()
    {
        LoadQuizData();
    }

    void LoadQuizData()
    {
        // load JSON file (has to be in 'Resources' file for it to work)
        TextAsset jsonFile = Resources.Load<TextAsset>("quiz_data"); 

        if (jsonFile != null)
        {
            quizData = JsonUtility.FromJson<QuizData>(jsonFile.text);
            Debug.Log("Quiz data loaded successfully!");

            // e.g. print the first question from the "easy" category
            if (quizData.questions.ContainsKey("easy") && quizData.questions["easy"].Count > 0)
            {
                Debug.Log("First Easy Question: " + quizData.questions["easy"][0].question);
            }
        }
        else
        {
            Debug.LogError("Failed to load JSON file! Make sure 'quiz_data.json' is in the Resources folder.");
        }
    }
}