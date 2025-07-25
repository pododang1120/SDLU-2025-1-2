﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SloltMachine : MonoBehaviour
{
    [Header("돈")]
    [SerializeField] private long credits = 100;

    [SerializeField] private TMP_InputField inputBetAmount;
    [SerializeField] private Image imageBetAmount;
    [SerializeField] private TextMeshProUGUI textCredits;
    [SerializeField] private TextMeshProUGUI _minBetText;
    private long _minBet;

    [Header("릴 텍스트")]
    [SerializeField] private TextMeshProUGUI[] reelTextsFlat = new TextMeshProUGUI[15];

    [Header("릴 이미지")]
    [SerializeField] private Image[] reelImagesFlat = new Image[15];


    [Header("카메라")]
    [SerializeField] private Transform cameraTransform;


    [Header("파티클")]
    [SerializeField] private ParticleSystem horizontalMatchParticle;

    [Header("배팅 배율")]
    [SerializeField] private int magnification;
    [SerializeField] private TextMeshProUGUI _magnificationText;

    [Header("남은 스핀 수")]
    [SerializeField] private TMPro.TextMeshProUGUI _numberOfSpinsreMaining;
    [SerializeField] private int _spin;


    #region 잭팟확률 관련
    private float jackpotChance = 0.05f;
    private const float jackpotChanceMax = 0.5f;
    private const float jackpotChanceIncrement = 0.005f;
    private const float jackpotChanceInitial = 0.05f;

    #endregion
    [SerializeField] private TextMeshProUGUI textResult;
    [SerializeField] private TextMeshProUGUI textChance;
    [SerializeField] private Button pullButton;
    [SerializeField] private Button allInButton;
    [SerializeField] private Button pButton;
    [SerializeField] private Button mButton;


    private Coroutine[] reelSpinCoroutines = new Coroutine[5];

    private int[,] reelResults = new int[3, 5];
    private Image[,] reelImages = new Image[3, 5];
    private TextMeshProUGUI[,] reelTexts = new TextMeshProUGUI[3, 5];

    private float spinDuration = 0.2f;
    private float elapsedTime = 0f;
    private bool isStartSpin = false;
    private bool isHorizontalMatchApplied = false;


    private bool[] isReelSpinned = new bool[5];

    Color32 customJackPot = new Color32(255, 239, 184, 255);

    private void Awake()
    {
        _minBet = credits / 20;
        credits = Math.Clamp(credits, 0, long.MaxValue / 2);
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                reelImages[row, col] = reelImagesFlat[row * 5 + col];
                reelTexts[row, col] = reelTextsFlat[row * 5 + col];
            }
        }
        UpdateMagnificationUI();
        textCredits.text = $"Credits : {credits.ToString("N0")}";
        _minBetText.text = $"Minimum bet \n {_minBet.ToString("N0")}";
        textChance.text = $"Jackpot Chance \n {jackpotChance * 10:F1}%";
        _magnificationText.text = $"Current Magnification\n" +
                                  $" Vertical : {magnification * 3}x" +
                                  $"\n Horizontal : {magnification * 4}x" +
                                  $"\n Jackpot : {magnification * 1000}x" +
                                  $"\n Fall : {magnification * 3}x";
        _numberOfSpinsreMaining.text = $"Number of spins remaining \n {_spin}";
    }

    private void Update()
    {
        if (!isStartSpin) return;

        elapsedTime += Time.deltaTime;

        for (int col = 0; col < 5; col++)
        {
            if (!isReelSpinned[col] && elapsedTime >= spinDuration)
            {
                ApplyVerticalMatch(col);
                isReelSpinned[col] = true;
                elapsedTime = 0f;
                break;
            }
        }

        if (AllReelsSpinned())
        {
            isStartSpin = false;
            ResetReelSpins();

            if (UnityEngine.Random.value < 0.1f)
                ApplyHorizontalMatch();

            UpdateReelDisplay();
            CheckBet();
            pullButton.interactable = true;
        }
    }

    private void ApplyVerticalMatch(int col)
    {
        int baseSpin = UnityEngine.Random.Range(1, 8);
        bool forceVerticalMatch = UnityEngine.Random.value < 0.2f;

        for (int row = 0; row < 3; row++)
        {
            reelResults[row, col] = forceVerticalMatch ? baseSpin : UnityEngine.Random.Range(1, 8);
        }
    }

    private void ApplyHorizontalMatch()
    {
        isHorizontalMatchApplied = false; // 초기화

        int matchRowCount = UnityEngine.Random.Range(1, 3); // 1~2줄 매칭
        List<int> rows = new List<int> { 0, 1, 2 };
        for (int i = 0; i < rows.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, rows.Count);
            (rows[i], rows[j]) = (rows[j], rows[i]);
        }

        for (int i = 0; i < matchRowCount; i++)
        {
            int row = rows[i];
            int value = UnityEngine.Random.Range(1, 8);
            for (int col = 0; col < 5; col++)
            {
                reelResults[row, col] = value;
            }
            isHorizontalMatchApplied = true;  // 가로매치 적용됨 표시
        }
    }


    private void ApplyJackpot()
    {
        int jackpotSymbol = UnityEngine.Random.Range(1, 8);

        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
                reelResults[row, col] = jackpotSymbol;
    }

    private void UpdateReelDisplay()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                reelTexts[row, col].text = reelResults[row, col].ToString("D1");
            }
        }
    }

    private void ResetReels()
    {
        foreach (var img in reelImagesFlat)
            img.color = Color.white;

        foreach (var txt in reelTextsFlat)
            txt.color = Color.black;

        OnMessage(Color.white, string.Empty);
    }

    public void OnMoney()
    {
        credits = 100000;
        textCredits.text = $"Credits : {credits.ToString("N0")}";
    }

    public void OnClickpull()
    {
        ResetReels();
        _spin -= 1;
        _numberOfSpinsreMaining.text = $"Number of spins remaining \n {_spin}";

        horizontalMatchParticle.Stop();
        if (!long.TryParse(inputBetAmount.text.Trim(), out long bet) || bet < _minBet)
        {
            OnMessage(Color.red, "Invalid bet amount");
            return;
        }

        if (credits < bet)
        {
            OnMessage(Color.red, "You don't have enough money");
            return;
        }

        credits -= bet;
        textCredits.text = $"Credits : {credits.ToString("N0")}";

        EnoughSpin();
    }

    public void EnoughSpin()
    {

        
        if (_spin <= 0)
        {
            StartSpin();
            pullButton.interactable = false;
            allInButton.interactable = false;
        }

    }

    public void OnClickP()
    {
        if (credits < 10)
        {
            OnMessage(Color.white, "You don't have enough money");
            return;
        }
        credits -= magnification * magnification;
        magnification = Mathf.Clamp(magnification + 1, 1, 20);

        UpdateMagnificationUI();
    }

    public void OnClickM()
    {
        if (credits < 10)
        {
            OnMessage(Color.white, "You don't have enough money");
            return;
        }
        credits -= magnification * 2;
        magnification = Mathf.Clamp(magnification - 1, 1, 20);

        UpdateMagnificationUI();
    }

    private void UpdateMagnificationUI()
    {
        // 버튼 상태 갱신
        mButton.interactable = magnification > 1;
        pButton.interactable = magnification < 20;

        if (magnification <= 2)
            _magnificationText.text = $"Current Magnification\n" +
                                      $" Vertical : {magnification * 3}x" +
                                      $"\n Horizontal : {magnification * 4}x" +
                                      $"\n Jackpot : {magnification * 1000}x" +
                                      $"\n Fall : {magnification * 3}x";

        else _magnificationText.text = $"Current Magnification\n" +
                              $" Vertical : {magnification * 3}x" +
                              $"\n Horizontal : {magnification * 4}x" +
                              $"\n Jackpot : {magnification * 1000}x" +
                              $"\n Fall : {(magnification + 5) * 3}x";

        textCredits.text = $"Credits : {credits:N0}";
    }

    private void StartSpin()
    {
        isStartSpin = true;
        pullButton.interactable = false;
        allInButton.interactable = false;
        elapsedTime = 0;
        ResetReelSpins();

        // 기본 랜덤 결과 생성
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
                reelResults[row, col] = UnityEngine.Random.Range(1, 8);

        // 세로줄 매치 확률 적용
        for (int col = 0; col < 5; col++)
        {
            if (UnityEngine.Random.value < 0.1f)
            {
                int val = UnityEngine.Random.Range(1, 8);
                for (int row = 0; row < 3; row++)
                    reelResults[row, col] = val;
            }
        }

        float rand = UnityEngine.Random.value;

        if (rand < jackpotChance)
        {
            ApplyJackpot();

        }
        else if (rand < jackpotChance + 0.5f)
        {
            ApplyHorizontalMatch();
            jackpotChance = Mathf.Min(jackpotChance + jackpotChanceIncrement, jackpotChanceMax);
        }
        else
        {
            jackpotChance = Mathf.Min(jackpotChance + jackpotChanceIncrement, jackpotChanceMax);
        }

        // 릴 스핀 시작
        for (int col = 0; col < 5; col++)
        {
            if (reelSpinCoroutines[col] != null)
                StopCoroutine(reelSpinCoroutines[col]);

            reelSpinCoroutines[col] = StartCoroutine(SpinReelLoop(col));
        }

        StartCoroutine(StopReelsOneByOne());
    }
    public void OnClickMinimumbet()
    {
        if (credits <= 0)
        {
            OnMessage(Color.red, "You have no credits");
            return;
        }

        inputBetAmount.text = _minBet.ToString();
        OnClickpull();
    }

    private void CheckBet()
    {
        long betAmount = long.Parse(inputBetAmount.text);
        bool hasMatch = false;

        foreach (var img in reelImagesFlat)
            img.color = Color.white;

        if (CheckJackpot(betAmount))
            return;

        bool vertical = CheckVertical(betAmount);
        bool horizontal = CheckHorizontal(betAmount);
        bool jackpot = CheckJackpot(betAmount);
        hasMatch = vertical || horizontal;

        _minBet = credits / 50;
        if (_minBet == 0)
            _minBet += 1;

        if (credits >= long.MaxValue / 2)
            CreditMaxOver();

        if (!hasMatch)
        {
            CheckFall(betAmount);
        }

        _minBetText.text = $"Minimum bet \n {_minBet.ToString("N0")}";

        textCredits.text = $"Credits : {credits.ToString("N0")}";
        textChance.text = $"Jackpot Chance \n {jackpotChance * 10:F1}%";
        textResult.text = hasMatch ? "YOU WIN!!!" : "YOU LOSE!!!!";

        if (horizontal || jackpot)
        {
            StartCoroutine(PlayHorizontalMatchEffects());
        }
    }
    #region 코루틴
    private IEnumerator BlinkText(TextMeshProUGUI text, float duration, float interval)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            text.enabled = !text.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        text.enabled = true;
    }

    private IEnumerator StartSpinSequence()
    {
        isStartSpin = true;
        pullButton.interactable = false;
        allInButton.interactable = false;

        ResetReelSpins();

        // 결과 랜덤 생성(필요시)
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
                reelResults[row, col] = UnityEngine.Random.Range(1, 8);

        for (int col = 0; col < 5; col++)
        {
            if (UnityEngine.Random.value < 0.3f)
            {
                int val = UnityEngine.Random.Range(1, 8);
                for (int row = 0; row < 3; row++)
                    reelResults[row, col] = val;
            }
        }

        if (UnityEngine.Random.value < 0.5f)
            ApplyHorizontalMatch();

        for (int col = 0; col < 5; col++)
        {
            yield return StartCoroutine(SpinReelCoroutine(col)); // 한 릴씩 순차적으로 스핀 & 멈춤
        }

        isStartSpin = false;

        yield return PlayHorizontalMatchEffects();

        CheckBet();
        pullButton.interactable = true;
        allInButton.interactable = true;
    }

    private IEnumerator SpinReelCoroutine(int col)
    {
        float spinTime = 0.8f; // 릴당 스핀 시간 조절
        float elapsed = 0f;
        float interval = 0.05f;

        while (elapsed < spinTime)
        {
            for (int row = 0; row < 3; row++)
            {
                int randVal = UnityEngine.Random.Range(1, 8);
                reelTexts[row, col].text = randVal.ToString();
            }
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // 최종 결과 표시
        for (int row = 0; row < 3; row++)
        {
            reelTexts[row, col].text = reelResults[row, col].ToString("D1");
        }

        isReelSpinned[col] = true;
    }


    private IEnumerator SpinReelLoop(int col)
    {
        while (!isReelSpinned[col])
        {
            for (int row = 0; row < 3; row++)
            {
                int randVal = UnityEngine.Random.Range(1, 8);
                reelTexts[row, col].text = randVal.ToString();
            }
            yield return new WaitForSeconds(0.05f);
        }

        // 최종 결과 표시
        for (int row = 0; row < 3; row++)
        {
            reelTexts[row, col].text = reelResults[row, col].ToString("D1");
        }
    }
    private IEnumerator StopReelsOneByOne()
    {
        for (int col = 0; col < 5; col++)
        {
            yield return new WaitForSeconds(0.2f); // 릴 간 멈추는 간격
            isReelSpinned[col] = true;             // 이 릴 멈춤
        }

        yield return new WaitForSeconds(0.2f);

        isStartSpin = false;

        CheckBet();
        pullButton.interactable = true;
        allInButton.interactable = true;
    }

    private IEnumerator PlayHorizontalMatchEffects()
    {
        if (isHorizontalMatchApplied)
        {
            // 파티클 재생 (예: particleSystem.Play();)
            horizontalMatchParticle.Play();

            // 화면 흔들기 효과 실행
            yield return StartCoroutine(ScreenShakeCoroutine(0.5f, 0.01f));
        }
    }

    private IEnumerator ScreenShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }
    #endregion
    private void OnMessage(Color color, string msg)
    {
        imageBetAmount.color = color;
        textResult.text = msg;
    }

    private bool CheckVertical(long bet)
    {
        bool matched = false;

        for (int col = 0; col < 5; col++)
        {
            int a = reelResults[0, col];
            int b = reelResults[1, col];
            int c = reelResults[2, col];

            if (a == b && b == c)
            {
                matched = true;
                credits += bet * (magnification * 3);

                for (int row = 0; row < 3; row++)
                {
                    reelImages[row, col].color = customJackPot;
                    StartCoroutine(BlinkText(reelTexts[row, col], 0.2f, 0.15f));
                }
            }
        }

        return matched;
    }

    private bool CheckHorizontal(long bet)
    {
        bool matched = false;

        for (int row = 0; row < 3; row++)
        {
            int a = reelResults[row, 0];
            int b = reelResults[row, 1];
            int c = reelResults[row, 2];
            int d = reelResults[row, 3];
            int e = reelResults[row, 4];

            if (a == b && b == c && c == d && d == e)
            {
                matched = true;
                credits += bet * (magnification * 4);

                for (int col = 0; col < 5; col++)
                {
                    reelImages[row, col].color = customJackPot;
                    StartCoroutine(BlinkText(reelTexts[row, col], 0.5f, 0.15f));
                }
            }
        }

        return matched;
    }

    private bool CheckJackpot(long betAmount)
    {
        int first = reelResults[0, 0];

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 5; c++)
                if (reelResults[r, c] != first)
                    return false;

        jackpotChance = jackpotChanceInitial;
        // 잭팟 처리
        textResult.text = " JACKPOT!!! ";
        credits += betAmount * (magnification * 1000);
        textCredits.text = $"Credits : {credits.ToString("N0")}";
        return true;
    }
    private bool CheckFall(long betAmount)
    {
        if (magnification <= 2)
            credits -= betAmount * magnification * 3;
        else
            credits -= betAmount * (magnification + 5) * 3;
        credits = Math.Clamp(credits, 0, long.MaxValue / 2);
        if (credits < 0)
        {
            CreditMinOver();
        }
        return true;
    }

    private void CreditMinOver()
    {
        credits = 0;
    }

    private void CreditMaxOver()
    {
        credits = long.MaxValue / 2;
    }

    private void ResetReelSpins()
    {
        for (int i = 0; i < 5; i++)
            isReelSpinned[i] = false;
    }

    private bool AllReelsSpinned()
    {
        foreach (bool b in isReelSpinned)
            if (!b) return false;
        return true;
    }
}
