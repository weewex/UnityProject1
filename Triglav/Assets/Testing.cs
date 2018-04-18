using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TestClass
{
    public string a;
    public string b;
}

public class Testing : MonoBehaviour {

    string testSplit = "Somedata|moreData|000011112222|12345";
    List<TestClass> list = new List<TestClass>();

	// Use this for initialization
	void Start () {
        TestClass someclass = new TestClass();
        someclass.a = "Test";
        someclass.b = "Random";
        TestClass someclass1 = new TestClass();
        someclass1.a = "John";
        someclass1.b = "Randolph";
        list.Add(someclass);
        list.Add(someclass1);
    }
	
	// Update is called once per frame
	void Update () {
        string[] d = testSplit.Split('|');
        Debug.Log("d.length = " + d.Length);
        for (int i = 0; i < d.Length; i++)
        {
            Debug.Log("d[" + i + "] = " + d[i]);
        }
        string msg = "TestClass|";
        foreach(TestClass a in list)
        {
            msg += a.a + '%' + a.b + '|';
        }
        msg.Trim('|');
        Debug.Log(msg);


	}
}
