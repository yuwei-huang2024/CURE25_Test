using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizCycleUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;         // textMeshPro for the current question
    public Button[] optionButtons;        // buttons for answer choices

    [Header("Timer Settings")]
    public TMP_Text timerText;           // text to display the countdown
    public float timePerQuestion = 10f;  // seconds per question
    private float currentTime;           // tracks time left for the current question
    private bool timerRunning = false;   // timer active?

    [Header("Hints")]
    public Button hintButton;            // button to use a hint (remove two wrong answers)
    private int hintsRemaining = 1;      // how many hints the player can use

    [Header("Explanation Panel")]
    public GameObject explanationPanel;  // panel to show explanation after each question
    public TMP_Text explanationText;     // display the explanation/fact
    public float explanationDuration = 2f; // seconds to show explanation before next question

    [Header("Completion Panel")]
    public GameObject quizCompletePanel; // panel to display when the quiz is finished
    public TMP_Text finalScoreText;      // "quiz Score: _/_"
    public Button keepLearningButton;    // "Keep Learning" button
    public Button mainMenuButton;        // "Go Back to Main Page" button

    private QuizDataLoader quizDataLoader;
    private List<QuizQuestion> currentQuestions;

    // Difficulty order
    private string[] difficulties = { "easy", "medium", "hard" };
    private int difficultyIndex = 0;   // difficulty we're on
    private int questionIndex = 0;     // question within the current difficulty

    // Score tracking
    private int totalQuestions = 0;    // total questions across all difficulties
    private int correctAnswers = 0;    // number correct answers

    // For controlling UI flow
    private bool showingFeedback = false; // showing feedback / explanation

    void Start()
    {
        quizDataLoader = FindObjectOfType<QuizDataLoader>();

        // hide completion & explanation panels at the start
        quizCompletePanel.SetActive(false);
        if (explanationPanel != null) explanationPanel.SetActive(false);

        // load questions from the first difficulty
        LoadDifficultyQuestions();
        DisplayQuestion();

        // setup hint button
        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintClicked);
            hintButton.gameObject.SetActive(hintsRemaining > 0);
        }

        // setup completion panel buttons
        keepLearningButton.onClick.AddListener(OnKeepLearningClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    void Update()
    {
        // handle timer countdown if active
        if (timerRunning && !showingFeedback)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                timerRunning = false;
                Debug.Log("Time's up!");
                // ran out of time -> treat as incorrect
                StartCoroutine(HandleAnswer(false, null, -1));
            }

            // update timer text
            if (timerText != null)
            {
                timerText.text = Mathf.Ceil(currentTime).ToString();
            }
        }
    }

    void LoadDifficultyQuestions()
    {
        if (difficultyIndex >= difficulties.Length)
        {
            Debug.Log("All difficulties completed. Quiz done!");
            ShowCompletionPanel();
            return;
        }

        currentQuestions = quizDataLoader.GetQuestionsByDifficulty(difficulties[difficultyIndex]);

        if (currentQuestions == null || currentQuestions.Count == 0)
        {
            Debug.Log("No questions found for " + difficulties[difficultyIndex] + ". Moving to next difficulty...");
            difficultyIndex++;
            LoadDifficultyQuestions();
        }
        else
        {
            questionIndex = 0;
            // add number of questions to totalQuestions
            totalQuestions += currentQuestions.Count;

            // randomize question order
            ShuffleList(currentQuestions);
        }
    }

    void DisplayQuestion()
    {
        // if currentQuestions is empty -> try next difficulty
        if (currentQuestions == null || currentQuestions.Count == 0)
        {
            difficultyIndex++;
            LoadDifficultyQuestions();
            if (currentQuestions == null || currentQuestions.Count == 0) return;
        }

        // if we've answered all questions in this difficulty -> move to next difficulty or show completion panel
        if (questionIndex >= currentQuestions.Count)
        {
            difficultyIndex++;
            LoadDifficultyQuestions();
            if (difficultyIndex >= difficulties.Length)
            {
                ShowCompletionPanel();
                return;
            }
            DisplayQuestion();
            return;
        }

        QuizQuestion q = currentQuestions[questionIndex];
        questionText.text = q.question;

        // randomize answer options
        ShuffleArray(q.options);

        // assign each answer button
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                btnText.text = q.options[i];
                optionButtons[i].onClick.RemoveAllListeners();

                int btnIndex = i;
                bool isCorrect = (q.options[i] == q.answer);

                optionButtons[i].onClick.AddListener(() => {
                    StartCoroutine(HandleAnswer(isCorrect, optionButtons[btnIndex], btnIndex));
                });
                optionButtons[i].gameObject.SetActive(true);

                // reset button color to default
                ResetButtonColor(optionButtons[i]);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // start the timer
        currentTime = timePerQuestion;
        timerRunning = true;

        // show hint button if we still have hints
        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(hintsRemaining > 0);
        }

        // hide explanation panel each time a new question appears
        if (explanationPanel != null) explanationPanel.SetActive(false);
        if (explanationText != null) explanationText.text = "";

        showingFeedback = false;
    }

    IEnumerator HandleAnswer(bool isCorrect, Button chosenButton, int chosenIndex)
    {
        // stop the timer
        timerRunning = false;
        showingFeedback = true;

        // find correct button index
        int correctButtonIndex = FindCorrectButtonIndex();
        QuizQuestion q = currentQuestions[questionIndex];

        // highlight chosen button and correct button
        if (isCorrect)
        {
            correctAnswers++;
            Debug.Log("Correct!");
            if (chosenButton != null) SetButtonColor(chosenButton, Color.green);
        }
        else
        {
            Debug.Log("Incorrect!");
            if (chosenButton != null) SetButtonColor(chosenButton, Color.red);

            // highlight correct button in green
            if (correctButtonIndex >= 0 && correctButtonIndex < optionButtons.Length)
            {
                SetButtonColor(optionButtons[correctButtonIndex], Color.green);
            }
        }

        // show explanation if available
        if (!string.IsNullOrEmpty(q.explanation))
        {
            if (explanationPanel != null) explanationPanel.SetActive(true);
            if (explanationText != null) explanationText.text = q.explanation;

            // keep the highlights for the entire duration of explanation text
            yield return new WaitForSeconds(explanationDuration);
        }
        else
        {
            // if no explanation, wait a little so user sees color on buttons
            yield return new WaitForSeconds(0.5f);
        }

        // after explanation reset button colors
        if (chosenButton != null) ResetButtonColor(chosenButton);
        if (!isCorrect && correctButtonIndex >= 0 && correctButtonIndex < optionButtons.Length)
        {
            ResetButtonColor(optionButtons[correctButtonIndex]);
        }

        // move to next question
        questionIndex++;
        DisplayQuestion();
        showingFeedback = false;
    }

    void ShowCompletionPanel()
    {
        // hide lingering explanation
        if (explanationPanel != null) explanationPanel.SetActive(false);

        // hide question text
        if (questionText != null) questionText.gameObject.SetActive(false);

        // hide timer
        if (timerText != null) timerText.gameObject.SetActive(false);

        // hide answer buttons
        foreach (Button btn in optionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }

        // hide hint button
        if (hintButton != null) hintButton.gameObject.SetActive(false);

        // finally show the completion panel
        quizCompletePanel.SetActive(true);
        finalScoreText.text = $"Quiz Score: {correctAnswers}/{totalQuestions}";

        Debug.Log($"Quiz Completed! Final Score: {correctAnswers}/{totalQuestions}");
    }

    void OnKeepLearningClicked()
    {
        Debug.Log("Keep Learning clicked!");
        // logic for new quiz, new topic (3d viewer), etc..
    }

    void OnMainMenuClicked()
    {
        Debug.Log("Go Back to Main Page clicked!");
        // logic to go back to a main page screen
    }

    void OnHintClicked()
    {
        if (hintsRemaining <= 0) return;

        QuizQuestion q = currentQuestions[questionIndex];
        // find all buttons that have a wrong answer
        List<int> wrongIndices = new List<int>();
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i].gameObject.activeSelf)
            {
                TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText.text != q.answer)
                {
                    wrongIndices.Add(i);
                }
            }
        }

        // remove 2 random wrong answers 
        if (wrongIndices.Count > 2)
        {
            ShuffleList(wrongIndices);
            optionButtons[wrongIndices[0]].gameObject.SetActive(false);
            optionButtons[wrongIndices[1]].gameObject.SetActive(false);
        }
        else
        {
            // if fewer than 2 wrong answers remain, hide them all
            foreach (int idx in wrongIndices)
            {
                optionButtons[idx].gameObject.SetActive(false);
            }
        }

        hintsRemaining--;
        if (hintButton != null && hintsRemaining <= 0)
        {
            hintButton.gameObject.SetActive(false);
        }

        Debug.Log("Hint used! Removed two wrong answers.");
    }

    // finds which button is the correct one for the current question
    int FindCorrectButtonIndex()
    {
        if (questionIndex < currentQuestions.Count)
        {
            QuizQuestion q = currentQuestions[questionIndex];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i].gameObject.activeSelf)
                {
                    TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                    if (btnText.text == q.answer) return i;
                }
            }
        }
        return -1;
    }

    // shuffle a list
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    // shuffle an array
    void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            T temp = array[i];
            array[i] = array[rand];
            array[rand] = temp;
        }
    }

    // permanently set a button's color (until reset)
    void SetButtonColor(Button btn, Color color)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.pressedColor = color;
        cb.selectedColor = color;
        cb.disabledColor = color;
        btn.colors = cb;
    }

    // reset a button to its default color settings
    void ResetButtonColor(Button btn)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = cb;
    }
}