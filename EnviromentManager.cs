using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class EnviromentManager : MonoBehaviour
{
    public GameObject agentsParent;
    public GameObject platformsParentPrefab;
    public List<MinerAgent> agents = new List<MinerAgent>();
    List<Vector3> initialAgentsPosition = new List<Vector3>();
    List<Quaternion> initialAgentsRotation = new List<Quaternion>();
    List<Platform> spawnedPlatforms = new List<Platform>();
    public GameObject platform;
    //public GameObject agent;
    public List<GameObject> walls;
    public Transform floor;
    public Transform mainCamera;
    List<Vector4> points = new List<Vector4>();
    GameObject platformsParent;
    //GameObject agentsParent;
    int numOfPlatforms;
    //int numOfAgents = 1;
    public int maxHitsNeeded;
    public int maxCoins;
    public int maxScale;
    public float agentsSpace;
    ulong steps = 0;
    ulong episodeDuration;
    int totalCoins = 0;
    EnvironmentParameters m_ResetParams;
    // Start is called before the first frame update
    void Start()
    {
        SetScene();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        steps++;
        //Debug.Log(((float)steps / (float)episodeDuration) * 100);
        bool platformsExist = false;
        bool coinsExist = false;
        for (int i = 0; i < spawnedPlatforms.Count; i++)
        {
            if(!spawnedPlatforms[i].isBroken())
            {
                platformsExist = true;
            }
            else
            {
                if(spawnedPlatforms[i].transform.childCount > 0)
                {
                    coinsExist = true;
                }
            }
        }
        
        if((!platformsExist && !coinsExist && steps > 1000) || (steps > episodeDuration))
        {
          
            ResetScene();
        }
    }

    void SetScene()
    {
        RandomiseParameters();
        DestroyOldPlatforms();
        SpawnPlatforms();
        agents = GetAgentsFromScene();

    }

    void ResetScene()
    {
        steps = 0;
        RandomiseParameters();
        ResetAgents();
        DestroyOldPlatforms();
        SpawnPlatforms();
        //agents = GetAgentsFromScene();
    }

    List<MinerAgent> GetAgentsFromScene()
    {
        List<MinerAgent> magents = new List<MinerAgent>();
        /*foreach (Transform tr in this.transform)
        {
            if(tr.CompareTag("Player"))
            {
                this.initialAgentsPosition.Add(tr.position);
                this.initialAgentsRotation.Add(tr.rotation);
                MinerAgent ma = tr.gameObject.GetComponent<MinerAgent>();
                ma.setNormHitsAndCoins(maxHitsNeeded, maxCoins);

                ma.setNormTotalCoins(totalCoins);
                magents.Add(ma);
            }
        }*/
        foreach (Transform child in agentsParent.transform)
        {
            this.initialAgentsPosition.Add(child.transform.position);
            this.initialAgentsRotation.Add(child.transform.rotation);
            MinerAgent ma = child.transform.gameObject.GetComponent<MinerAgent>();
            ma.setNormHitsAndCoins(maxHitsNeeded, maxCoins);

            ma.setNormTotalCoins(totalCoins);
            magents.Add(ma);

            Debug.Log("Name Of Child " + child.transform.name);
        }
        return magents;
    }

    void ResetAgents()
    {
        int idx = 0;
        foreach (var agent in agents)
        {
            agent.EndEpisode();
            agent.transform.position = initialAgentsPosition[idx];
            agent.transform.rotation = initialAgentsRotation[idx];
            agent.rBody.angularVelocity = Vector3.zero;
            agent.rBody.velocity = Vector3.zero;
            agent.setNormHitsAndCoins(maxHitsNeeded, maxCoins);
            idx++;
        }

    }


    void RandomiseParameters()
    {
        
        int size = Random.Range(6, 20);
        maxCoins = size;
        //de modificat dimensiunea platformelor sa incapa pt orice size
        maxScale = 6 * (int)System.Math.Sqrt((double)size);
        maxHitsNeeded = 2 * size;
        
        numOfPlatforms = 2 * size;
        agentsSpace = size;

        episodeDuration = (ulong)(25 * size * size);

        float posx = (size - 4) * (56 - 28) / (8 - 4) + 28;
        float posy = (size - 4) * (40 - 21) / (8 - 4) + 21;
        mainCamera.position = new Vector3(posx, posy, -1f);
        //change floor size
        floor.localScale = new Vector3(size * 10,
                                       0.5f,
                                       size * 10);
        //walls around floor
        BuildWalls(size);
    }

    void SpawnPlatforms()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        var curriculum_hitsNeeded = m_ResetParams.GetWithDefault("hitsNeeded", -1f); //-1 default for hitsNeeded based on distance from center
        totalCoins = 0;

        spawnedPlatforms.Clear();
        points.Clear();
        platformsParent = Instantiate(platformsParentPrefab, this.transform);
        platformsParent.transform.SetParent(this.transform);
        int platformsSpawned = 0;
        
        float xSize = floor.lossyScale.x - maxScale;
        float zSize = floor.lossyScale.z - maxScale;
        float xPos = floor.position.x;
        float zPos = floor.position.z;
        float xMin = xPos - xSize / 2;
        float xMax = xPos + xSize / 2;
        float zMin = zPos - zSize / 2;
        float zMax = zPos + zSize / 2;
        float maxDistanceFromCenter = (float)System.Math.Sqrt(System.Math.Pow((xSize / 2), 2) + System.Math.Pow((zSize / 2), 2));

        //Add center as space where platforms can't be so the agents start there
        points.Add(new Vector4(-agentsSpace, -agentsSpace, agentsSpace, agentsSpace));

        int tries = 0;
        while (platformsSpawned < numOfPlatforms && tries < 1000)
        {
            tries++;
            float platformX = Random.Range(xMin, xMax);
            float platformZ = Random.Range(zMin, zMax);

            float distanceFromCenter = (float)System.Math.Sqrt(System.Math.Pow((xPos - platformX), 2) + System.Math.Pow((zPos - platformZ), 2));

            float platformScale = distanceFromCenter / maxDistanceFromCenter * maxScale;//in functie de distanta pana la centru
            int numOfCoins = (int)(distanceFromCenter / maxDistanceFromCenter * maxCoins); //in functie de distanta pana la centru
            int hitsNeeded;
            if(curriculum_hitsNeeded == -1)
            {
                hitsNeeded = 2 * numOfCoins;
            }
            else
            {
                hitsNeeded = (int)curriculum_hitsNeeded;
            }
            bool overlap = false;

            Vector3 platformPos = new Vector3(platformX,
                                       this.transform.position.y + platform.transform.lossyScale.y / 2, platformZ);
            Vector4 platformBounds = new Vector4(platformPos.x - platformScale / 2,
                                                     platformPos.z - platformScale / 2,
                                                     platformPos.x + platformScale / 2,
                                                     platformPos.z + platformScale / 2);
            for (int i = 0; i < points.Count; ++i)
            {

                if (DoOverlap(points[i], platformBounds))
                {
                    overlap = true;
                }
            }
            if (overlap == false)
            {
                points.Add(platformBounds);
                platform.transform.localScale = new Vector3(platformScale, 1, platformScale);
                platform.transform.position = platformPos;
                Platform platformClone = Instantiate(platform, platformsParent.transform, true).GetComponent<Platform>(); 
                platformClone.setNumOfCoins(numOfCoins);
                totalCoins += numOfCoins;
                platformClone.setHitsNeeded(hitsNeeded);
                if(hitsNeeded == 0)
                {
                    platformClone.breakPlatform();
                }
                platformsSpawned++;
                spawnedPlatforms.Add(platformClone);
            }
        }
    }

    void DestroyOldPlatforms()
    {
        Destroy(platformsParent);
        totalCoins = 0;
    }

    bool DoOverlap(Vector4 r1, Vector4 r2)
    {
        if (r1.x == r1.z || r1.y == r1.w || r2.x == r2.z || r2.y == r2.w)
        {
            return false;
        }
        if (r1.x > r2.z || r2.x > r1.z)
        {
            return false;
        }
        if (r1.y > r2.w || r2.y > r1.w)
        {
            return false;
        }
        return true;
    }

    void BuildWalls(int size)
    {
        float wallLen = size * 10f + 2f;
        walls[0].transform.localPosition = new Vector3(size * 5f + 0.5f, 2.5f, 0f);
        walls[1].transform.localPosition = new Vector3(-(size * 5f + 0.5f), 2.5f, 0f);
        walls[2].transform.localPosition = new Vector3(0f, 2.5f, size * 5f + 0.5f);
        walls[3].transform.localPosition = new Vector3(0f, 2.5f, -(size * 5f + 0.5f));

        walls[0].transform.localScale = new Vector3(wallLen, 6, 1);
        walls[1].transform.localScale = new Vector3(wallLen, 6, 1);
        walls[2].transform.localScale = new Vector3(wallLen, 6, 1);
        walls[3].transform.localScale = new Vector3(wallLen, 6, 1);

    }

    public int getMaxCoins()
    {
        return maxCoins;
    }

    public int getMaxHits()
    {
        return maxHitsNeeded;
    }
}
