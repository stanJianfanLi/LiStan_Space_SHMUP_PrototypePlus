using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Part is another serializable data storage class just like WeaponDefinition
[System.Serializable]
public class Part
{
    // These three fields need to be defined in the Inspector pane
    public string name;         // The name of this part
    public float health;       // The amount of health this part has
    public string[] protectedBy;  // The other parts that protect this

    // These two fields are set automatically in Start().
    // Caching like this makes it faster and easier to find these later
    [HideInInspector]
    public GameObject go;           // The GameObject of this part
    [HideInInspector]
    public Material mat;          // The Material to show damage
}

public class Enemy4 : Enemy
{
    // Enemy_4 will start offscreen and then pick a random point on screen to
    //   move to. Once it has arrived, it will pick another random point and
    //   continue until the player has shot it down.
    [Header("Set inInspector: Enemy_4")]
    public Part[] parts;

    public Vector3 p0, p1;  // Stores the p0 & p1 for interpolation
    public float timeStart;  // Birth time for this Enemy_4
    public float duration = 4;  // Duration of movement

    void Start()
    {
        p0 = p1 = pos;
        InitMovement();

        Transform t;
        foreach (Part prt in parts)
        {
            t = transform.Find(prt.name);
            if (t != null)
            {
                prt.go = t.gameObject;
                prt.mat = prt.go.GetComponent<Renderer>().material;
            }
        }
    }

    void InitMovement()
    {
        // Pick a new point to move to that is on screen
        p0 = p1;
        float widMinRad = bndCheck.camWidth - bndCheck.radius;
        float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
        p1.x = Random.Range(-widMinRad, widMinRad);
        p1.y = Random.Range(-hgtMinRad, hgtMinRad);

        // Reset the time
        timeStart = Time.time;
    }

    public override void Move()
    {
        // This completely overrides Enemy.Move() with a linear interpolation

        float u = (Time.time - timeStart) / duration;
        if (u >= 1)
        {  // if u >=1...
            InitMovement();  // ...then initialize movement to a new point
            u = 0;
        }

        u = 1 - Mathf.Pow(1 - u, 2);         // Apply Ease Out easing to u

        pos = (1 - u) * p0 + u * p1; // Simple linear interpolation
    }
    Part FindPart(string n)
    {
        foreach (Part prt in parts)
        {
            if (prt.name == n)
            {
                return (prt);
            }
        }
        return (null);
    }
    Part FindPart(GameObject go)
    {
        foreach (Part prt in parts)
        {
            if (prt.go == go)
            {
                return (prt);
            }
        }
        return (null);
    }
    bool Destroyed(GameObject go)
    {
        return (Destroyed(FindPart(go)));
    }
    bool Destroyed(string n)
    {
        return (Destroyed(FindPart(n)));
    }
    bool Destroyed(Part prt)
    {
        if (prt == null)
        {
            return (true);
        }
        return (prt.health <= 0);
    }
    void ShowLocalizedDamage(Material m)
    {
        m.color = Color.red;
        damageDoneTime = Time.time + showDamageDuration;
        showingDamage = true;
    }
    // This will override the OnCollisionEnter that is part of Enemy.cs
    // Because of the way that MonoBehaviour declares common Unity functions
    //   like OnCollisionEnter(), the override keyword is not necessary.
    void OnCollisionEnter(Collision coll)
    {
        GameObject other = coll.gameObject;
        switch (other.tag)
        {
            case "ProjectileHero":
                Projectile p = other.GetComponent<Projectile>();
                // Enemies don't take damage unless they're on screen
                // This stops the player from shooting them before they are visible
                if (!bndCheck.isOnScreen)
                {
                    Destroy(other);
                    break;
                }

                // Hurt this Enemy
                // Find the GameObject that was hit
                // The Collision coll has contacts[], an array of ContactPoints
                // Because there was a collision, we're guaranteed that there is at
                //   least a contacts[0], and ContactPoints have a reference to
                //   thisCollider, which will be the collider for the part of the
                //   Enemy_4 that was hit.
                GameObject goHit = coll.contacts[0].thisCollider.gameObject;
                Part prtHit = FindPart(goHit);
                if (prtHit == null)
                { // If prtHit wasn't found
                  //   ...then it's usually because, very rarely, thisCollider on
                  //   contacts[0] will be the ProjectileHero instead of the ship
                  //   part. If so, just look for otherCollider instead
                    goHit = coll.contacts[0].otherCollider.gameObject;
                    prtHit = FindPart(goHit);
                }
                // Check whether this part is still protected
                if (prtHit.protectedBy != null)
                {
                    foreach (string s in prtHit.protectedBy)
                    {
                        // If one of the protecting parts hasn't been destroyed...
                        if (!Destroyed(s))
                        {
                            // ...then don't damage this part yet
                            Destroy(other);  // Destroy the ProjectileHero
                            return; // return before causing damage
                        }
                    }
                }
                // It's not protected, so make it take damage
                // Get the damage amount from the Projectile.type & Main.W_DEFS
                prtHit.health -= Main.GetWeaponDefinition(p.type).damageOnHit;
                // Show damage on the part
                ShowLocalizedDamage(prtHit.mat);
                if (prtHit.health <= 0)
                {
                    // Instead of Destroying this enemy, disable the damaged part
                    prtHit.go.SetActive(false);
                }
                // Check to see if the whole ship is destroyed
                bool allDestroyed = true; // Assume it is destroyed
                foreach (Part prt in parts)
                {
                    if (!Destroyed(prt))
                    { // If a part still exists
                        allDestroyed = false; // ...change allDestroyed to false
                        break;                // and break out of the foreach loop
                    }
                }
                if (allDestroyed)
                { // If it IS completely destroyed
                  // Tell the Main singleton that this ship has been destroyed
                    Main.S.ShipDestroyed(this);
                    // Destroy this Enemy
                    Destroy(this.gameObject);
                }
                Destroy(other);  // Destroy the ProjectileHero
                break;
        }
    }
}
