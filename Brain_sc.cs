using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// Replay sınıfı, bir durum ve ona ait ödülü tutar
public class Replay
{
    public List<double> states;// Durum bilgilerini tutar
    public double reward; // Duruma ait ödül
    public Replay(double xr, double ballz, double ballvx, double r)
    {
        states = new List<double>();
        states.Add(xr);
        states.Add(ballz);
        states.Add(ballvx);
        reward = r;
    }
}
// Brain_sc sınıfı, yapay sinir ağıyla top dengeleme oyununu kontrol eder
public class Brain_sc : MonoBehaviour {
    public GameObject ball;             //objeyi izlemek için
    ANN ann;
    float reward = 0.0f;                //hareketlere ödül atamak için
    List<Replay> replayMemory = new List<Replay>(); //geçmiş hareketler ve ödüllerin listesi
    int mCapacity = 10000;              //hafıza kapasitesi
    float discount = 0.99f;             //gelecek durumların ödüllere etkisi
    float exploreRate = 100.0f;         //rastgele hareket seçme olasılığı
    float maxExploreRate = 100.0f;      //maksimum olasılık değeri
    float minExploreRate = 0.01f;       //minimum olasılık değeri
    float exploreDecay = 0.0001f;       //her güncellemede olasılık azalma miktarı
    Vector3 ballStartPos;               //objenin başlangıç pozisyonunu kaydetme
    int failCount = 0;                  //top düştüğünde sayma
    float tiltSpeed = 0.5f;             //her güncellemede eğme açısı
    float timer = 0;                    //Topun dengede tutulduğu süre
    float maxBalanceTime = 0;           //topun dengede tutulduğu süreyi kaydetme

    //Başlangıç için
    void Start() {
        // Yapay sinir ağı (3 giriş, 2 çıkış, 1 gizli katman, 6 nöron, öğrenme oranı 0.02)
        ann = new ANN(3,2,1,6,0.02f);
        ballStartPos = ball.transform.position;
        Time.timeScale = 5.0f;
    }
    // GUI elemanları için stil
    GUIStyle guiStyle = new GUIStyle();
    // İstatistiklerin GUI'de gösterimi
    void OnGUI()
    {
        guiStyle.fontSize = 25;
        guiStyle.normal.textColor = Color.white;
        GUI.BeginGroup(new Rect(10, 10, 600, 150));
        GUI.Box(new Rect(0, 0, 140, 140), "Stats", guiStyle);
        GUI.Label(new Rect(10, 25, 500, 30), "Fails: " + failCount, guiStyle);
        GUI.Label(new Rect(10, 50, 500, 30), "Decay Rate: " + exploreRate, guiStyle);
        GUI.Label(new Rect(10, 75, 500, 30), "Last Best Balance: " + maxBalanceTime, guiStyle);
        GUI.Label(new Rect(10, 100, 500, 30), "This Balance: " + timer, guiStyle);
        GUI.EndGroup();
    }
    // Oyuncunun "space" tuşuna basması durumunda top sıfırlanır
    void Update()
    {
        if (Input.GetKeyDown("space"))
            ResetBall();
    }
    // Fizik tabanlı güncelleme
    void FixedUpdate () 
    {
        timer += Time.deltaTime;
        List<double> states = new List<double>();
        List<double> qs = new List<double>();
        // Mevcut durumları hesapla ve listeye ekle
        states.Add(this.transform.rotation.x);
        states.Add(ball.transform.position.z);
        states.Add(ball.GetComponent<Rigidbody>().angularVelocity.x);
        // ANN'den çıktı al ve softmax uygula
        qs = SoftMax(ann.CalcOutput(states));
        double maxQ = qs.Max();// En yüksek Q değeri
        int maxQIndex = qs.ToList().IndexOf(maxQ);// En iyi hareketin indeksi
        // Keşif oranını azalt
        exploreRate = Mathf.Clamp(exploreRate - exploreDecay, minExploreRate, maxExploreRate);
        // En iyi harekete göre platformu eğ
        if (maxQIndex == 0)
            this.transform.Rotate(Vector3.right, tiltSpeed * (float)qs[maxQIndex]);
        else if (maxQIndex == 1)
            this.transform.Rotate(Vector3.right, -tiltSpeed * (float)qs[maxQIndex]);
        // Ödül hesaplama
        if (ball.GetComponent<BallState>().dropped)
            reward = -1.0f;
        else
            reward = 0.1f;
        // Yeni hareketi hafızaya ekle
        Replay lastMemory = new Replay(this.transform.rotation.x,
                                ball.transform.position.z,
                                ball.GetComponent<Rigidbody>().angularVelocity.x,
                                reward);
        if(replayMemory.Count > mCapacity)
            replayMemory.RemoveAt(0);
        replayMemory.Add(lastMemory);
        // Top düşerse geçmiş hareketleri analiz et ve sinir ağını eğit
        if(ball.GetComponent<BallState>().dropped)
        {
            for(int i = replayMemory.Count - 1; i >= 0; i--)
            {
                List<double> toutputsOld = new List<double>();
                List<double> toutputsNew = new List<double>();
                toutputsOld = SoftMax(ann.CalcOutput(replayMemory[i].states));

                double maxQOld = toutputsOld.Max();
                int action = toutputsOld.ToList().IndexOf(maxQOld);

                double feedback;
                if(i == replayMemory.Count-1 || replayMemory[i].reward == -1)
                    feedback = replayMemory[i].reward;
                else
                {
                    toutputsNew = SoftMax(ann.CalcOutput(replayMemory[i+1].states));
                    maxQ = toutputsNew.Max();
                    feedback = replayMemory[i].reward + discount * maxQ;
                }
                toutputsOld[action] = feedback;
                ann.Train(replayMemory[i].states, toutputsOld);
            }
            if (timer > maxBalanceTime)
            {
                maxBalanceTime = timer;
            }
            timer = 0;
            ball.GetComponent<BallState>().dropped = false;
            this.transform.rotation = Quaternion.identity;
            ResetBall();
            replayMemory.Clear();
            failCount++;
        }
    }
    // Topu başlangıç pozisyonuna sıfırlar
    void ResetBall()
    {
        ball.transform.position = ballStartPos;
        ball.GetComponent<Rigidbody>().linearVelocity = new Vector3(0,0,0);
        ball.GetComponent<Rigidbody>().angularVelocity = new Vector3(0,0,0);
    }
    // SoftMax fonksiyonu, Q değerlerini olasılıklara dönüştürür
    List<double> SoftMax(List<double> values)
    {
        double max = values.Max();
        float scale = 0.0f;
        for (int i = 0; i < values.Count; ++i)
            scale += Mathf.Exp((float)(values[i] - max));
        List<double> result = new List<double>();
        for (int i = 0; i < values.Count; ++i)
            result.Add(Mathf.Exp((float)(values[i] - max)) / scale);
        return result;
    }
}