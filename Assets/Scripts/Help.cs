using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Help : MonoBehaviour
{
    public int page = 0;

    public GameObject[] pages;

    public GameObject helpContainer;

    public void openHelp() {
        this.helpContainer.SetActive(true);
        this.setPageActive();
    }
    public void closeHelp() {
        this.helpContainer.SetActive(false);
    }

    public void nextPage() {
        if (!((this.page + 1) >= this.pages.Length)) {
            this.page += 1;
            this.setPageActive();
        }
    }

    public void previousPage() {
        if (!((this.page - 1) < 0)) {
            this.page -= 1;
            this.setPageActive();
        }
    }

    private void setPageActive() {
        for (int i = 0; i < this.pages.Length; i++)
        {
            this.pages[i].SetActive(this.page == i);
        }
    }
}
