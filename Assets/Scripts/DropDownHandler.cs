using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class DropDownHandler : MonoBehaviour
{
    private Dropdown dropdown;
    DancerToBoidController cmpController;

    dancerToBoidPreset _preset;

    private void Awake()
    {
        cmpController = FindObjectOfType<DancerToBoidController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        dropdown = transform.GetComponent<Dropdown>();
        dropdown.options.Clear();
        
        fillDropDown();
    }

    public void fillDropDown()
    {
        var values = Enum.GetValues(typeof(dancerToBoidPreset));

        foreach (dancerToBoidPreset item in values)
        {
            dropdown.options.Add(new Dropdown.OptionData { text = item.ToString() });
        }

        dropdown.onValueChanged.AddListener(delegate { ChangeDropdown(); });
    }

    void ChangeDropdown()
    {
        int index = dropdown.value;
        cmpController.ChangeBoidValuesOnce(index);

    }
}
