using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    
    public GameObject coin;
    public int hitsNeeded = 5;
    public int meanReward = 5;
    public int numOfCoins;
    float coinDiameter = 1f;
    List<Vector3> points = new List<Vector3>();
    bool broken = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }



    public void Hit()
    {
        if(!broken)
        {
            hitsNeeded--;
            print(string.Format("{0} hits to go!", hitsNeeded));
            if (hitsNeeded <= 0)
            {
                breakPlatform();
            }
        }
        
    }

    public void spawnCoins()
    {
        
        int coinsSpawned = 0;
        float xSize = this.transform.lossyScale.x;
        float zSize = this.transform.lossyScale.z;
        float xPos = this.transform.position.x;
        float zPos = this.transform.position.z;
        float xMin = xPos - xSize / 2;
        float xMax = xPos + xSize / 2;
        float zMin = zPos - zSize / 2;
        float zMax = zPos + zSize / 2;

        while (coinsSpawned < numOfCoins)
        {
            bool overlap = false;
            Vector3 coinPos = new Vector3(Random.Range(xMin, xMax),
                                       this.transform.position.y + 2f, Random.Range(zMin, zMax));
            for(int i = 0; i < coinsSpawned; ++i)
            {
                //check distance between centers of points
                if ((coinPos - points[i]).sqrMagnitude < coinDiameter * coinDiameter)
                {
                    overlap = true;
                }
            }
            if(overlap == false)
            {
                points.Add(coinPos);
                coin.transform.position = coinPos;
                Instantiate(coin, this.transform, true);
                coinsSpawned++;
            }
        }
        
        
    }

    public int getHits()
    {
        return this.hitsNeeded;
    }
  
    public int getNumOfCoins()
    {
        return this.numOfCoins;
    }

    public bool isBroken()
    {
        return this.broken;
    }

    public void setBroken(bool broken)
    {
        this.broken = broken;
    }

    public void setNumOfCoins(int setNumOfCoins)
    {
        this.numOfCoins = setNumOfCoins;
    }

    public void setHitsNeeded(int hits)
    {
        this.hitsNeeded = hits;
    }


    public void breakPlatform()
    {
        broken = true;
        Vector3 offset = new Vector3(0, 2, 0);
        transform.position -= offset;
        spawnCoins();
    }

    
}
