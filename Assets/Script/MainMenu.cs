using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // ฟังก์ชันสำหรับปุ่ม Play
    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
        Debug.Log("Game Start");
    }

    public void Options()
    {

    }
    public void QuitGame()
    {
        Debug.Log("ออกจากเกม!");
        Application.Quit(); // คำสั่งนี้จะทำงานตอน Build เกมเป็นไฟล์ .exe แล้วเท่านั้น
    }
}
