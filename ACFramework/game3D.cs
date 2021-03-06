using System;
using System.Drawing;
using System.Windows.Forms;

// mod: setRoom1 doesn't repeat over and over again

namespace ACFramework
{

    class cCritterDoor : cCritterWall
    {

        public cCritterDoor(cVector3 enda, cVector3 endb, float thickness, float height, cGame pownergame)
            : base(enda, endb, thickness, height, pownergame)
        {
        }

        public override bool collide(cCritter pcritter)
        {
            bool collided = base.collide(pcritter);
            if (collided && pcritter.IsKindOf("cCritter3DPlayer"))
            {
                ((cGame3D)Game).setdoorcollision();
                return true;
            }
            return false;
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritterDoor" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterDoor";
            }
        }
	} 
	
    //Class for moving wall
    class cCritterMovingWall : cCritterWall
    {
        private bool backwards, down, up;
        private const int PAUSE_LENGTH = 500;
        private int paused;//will pause the moving of the wall for a short period of time
        public float MOVING_SPEED = 0.01f;
        public cCritterMovingWall(cVector3 enda, cVector3 endb, float thickness, float height, cGame pownergame)
            : base(enda, endb, thickness, height, pownergame)
        {
            backwards = false;
            down = false;
            up = false;
            paused = -1;
        }
        public void moveWall()
        {
            if (paused > 0)
            {
                paused--;
                if (paused < 0)
                {
                    up = true;
                }
            }
            else if (!backwards && !down && !up)//moving forward
            {
                //move the wall forward by 1 z value
                moveTo(Position.add(new cVector3(0.0f, 0.0f, MOVING_SPEED)));
                if(Position.Z > 15.0f)
                {
                    down = true;
                }
            }
            else if(!down && !up)//moving backward
            {
                //move the wall backward by 1 z value
                moveTo(Position.add(new cVector3(0.0f, 0.0f, -MOVING_SPEED)));
                if(Position.Z < -15.0f)
                {
                    backwards = false;
                    up = false;
                    down = true;
                }
            }
            else if(down)//moving down
            {
                moveTo(Position.add(new cVector3(0.0f, -MOVING_SPEED, 0.0f)));
                if(Position.Y < -7.0f)
                {
                    up = true;
                    down = false;
                    paused = PAUSE_LENGTH;
                }
            }
            else if(up)//moving up
            {
                moveTo(Position.add(new cVector3(0.0f, MOVING_SPEED, 0.0f)));
                if(Position.Y > 5.0f && Position.Z > 15.0f)
                {
                    up = false;
                    down = false;
                    backwards = true;
                }
                if(Position.Y > 5.0f && Position.Z < -15.0f)
                {
                    up = false;
                    down = false;
                    backwards = false;
                }
            }


        }
        public override bool IsKindOf(string str)
        {
            return str == "cCritterMovingWall" || base.IsKindOf(str);
        }
        public override string RuntimeClass
        {
            get
            {
                return "cCritterMovingWall";
            }
        }
    }

	//==============Critters for the cGame3D: Player, Ball, Treasure ================ 
	
	class cCritter3DPlayer : cCritterArmedPlayer 
	{ 
        private cGame3D owner;
        private bool gohanSpawned;
        public static int numDragBallsCollected;
        public char Mode;
        public cCritter3DPlayer( cGame pownergame ) 
            : base( pownergame ) 
		{
            BulletClass = new cCritter3DPlayerBullet();
            Sprite = new cSpriteQuake(ModelsMD2.Goku); 
			//Sprite.FillColor = Color.DarkGreen; 
			Sprite.SpriteAttitude = cMatrix3.scale( 2, 0.8f, 0.4f ); 
			setRadius( 0.42f ); //Default cCritter.PLAYERRADIUS is 0.4.  
			setHealth( 10 ); 
			moveTo( _movebox.Center.add( new cVector3( 0.0f, 0.0f, 16.0f ))); 
			WrapFlag = cCritter.CLAMP; //Use CLAMP so you stop dead at edges.
			Armed = true; //Let's use bullets.
			MaxSpeed =  cGame3D.MAXPLAYERSPEED + 20; 
			AbsorberFlag = true; //Keeps player from being buffeted about.
			ListenerAcceleration = 160.0f; //So Hopper can overcome gravity.  Only affects hop.
            owner = pownergame as cGame3D;
            // YHopper hop strength 12.0
			Listener = new cListenerQuakeScooterYHopper( 0.2f, 15.0f );
            // the two arguments are walkspeed and hop strength -- JC
            gohanSpawned = false;
            addForce( new cForceGravity( 50.0f )); /* Uses  gravity. Default strength is 25.0.
			Gravity	will affect player using cListenerHopper. */ 
			AttitudeToMotionLock = false; //It looks nicer is you don't turn the player with motion.
			Attitude = new cMatrix3( new cVector3(0.0f, 0.0f, -1.0f), new cVector3( -1.0f, 0.0f, 0.0f ), 
                new cVector3( 0.0f, 1.0f, 0.0f ), Position);
            numDragBallsCollected = 0;
		}

        public override void update(ACView pactiveview, float dt)
        {
            base.update(pactiveview, dt); //Always call this first
 
        } 

        public override bool collide( cCritter pcritter ) 
		{
            
			bool playerhigherthancritter = Position.Y - Radius > pcritter.Position.Y; 
		/* If you are "higher" than the pcritter, as in jumping on it, you get a point
	and the critter dies.  If you are lower than it, you lose health and the
	critter also dies. To be higher, let's say your low point has to higher
	than the critter's center. We compute playerhigherthancritter before the collide,
	as collide can change the positions. */
            _baseAccessControl = 1;
			bool collided = base.collide( pcritter );
            _baseAccessControl = 0;
            if (!collided) 
				return false;
		/* If you're here, you collided.  We'll treat all the guys the same -- the collision
	 with a Treasure is different, but we let the Treasure contol that collision. */ 
			if ( playerhigherthancritter ) 
			{
                Framework.snd.play(Sound.Goopy); 
				addScore( 10 ); 
			}
            else if (pcritter.IsKindOf("cCritter3DBoss"))
            {
                damage(2);
            }
            else if(pcritter.IsKindOf("cCritterWank"))
            {
                if (gohanSpawned)
                {
                    return false;
                }
                else
                {
                    //spawn little goku
                    cCritterGohan gohan = new cCritterGohan(owner);
                    gohan.moveTo(new cVector3(20.0f, 0.0f, -22.0f));
                    gohanSpawned = true;
                    MessageBox.Show("Your son, Gohan, has been born.");
                    return true;
                }
            }
            else if(pcritter.IsKindOf("cCritterGohan"))
            {
                //play gohan sound
                return true;
            }
            else if(pcritter.IsKindOf("cCritterDragonball"))
            {
                int rand = (int)Framework.randomOb.random(3);
                switch (rand)
                {
                    case 0:
                        Framework.snd.play(Sound.Samurai);
                        break;
                    case 1:
                        Framework.snd.play(Sound.Shout);
                        break;
                    case 2:
                        Framework.snd.play(Sound.WorkHard);
                        break;


                }
                addScore(100);
                addHealth(5);
                numDragBallsCollected++;
                pcritter.die();
                return true;
            }
            else
            { 
				damage( 1 );
                Framework.snd.play(Sound.Crunch);
                pcritter.die();

            } 
			return true; 
		}

        public override cCritterBullet shoot()
        {
                Framework.snd.play(Sound.LaserFire);
                return base.shoot();
        }

        public override bool IsKindOf( string str )
        {
            return str == "cCritter3DPlayer" || base.IsKindOf( str );
        }
		
        public int NumDragonBallsCollected
        {
            get { return numDragBallsCollected; }
            set { numDragBallsCollected = value; }
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritter3DPlayer";
            }
        }
	}

    

    class cCritter3DPlayerBullet : cCritterBullet 
	{

        public cCritter3DPlayerBullet() { }

        public override cCritterBullet Create()
            // has to be a Create function for every type of bullet -- JC
        {
            return new cCritter3DPlayerBullet();
        }
		
		public override void initialize( cCritterArmed pshooter ) 
		{ 
			base.initialize( pshooter );
            if (((cCritter3DPlayer) pshooter).Mode == 'K')
            {
                Sprite = new cSpriteSphere();
                Sprite.FillColor = Color.Blue;
                setRadius(0.4f);
                HitStrength = 2;
            }
            else if (((cCritter3DPlayer)pshooter).Mode == 'S')
            {
                Sprite.FillColor = Color.Crimson;
                // can use setSprite here too
                setRadius(0.1f);
            }
		}
        public override bool collide(cCritter pcritter)
        {
            if (pcritter.IsKindOf("cCritterWank") || pcritter.IsKindOf("cCritterDragonball"))
            {
                return false;
            }
            else
            {
                return base.collide(pcritter);
            }
       }

        public override bool IsKindOf( string str )
        {
            return str == "cCritter3DPlayerBullet" || base.IsKindOf( str );
        }
		
        public override string RuntimeClass
        {
            get
            {
                return "cCritter3DPlayerBullet";
            }
        }
	}

    class cCritterBulletKamehameha : cCritterBulletSilver
    {
        public cCritterBulletKamehameha()
        {
            _shooterindex = cBiota.NOINDEX;
            _hitstrength = 2;
            _dieatedges = true;
            _defaultprismdz = cSprite.BULLETPRISMDZ;
            _value = 0;
            _usefixedlifetime = true;
            _fixedlifetime = FIXEDLIFETIME;
            _collidepriority = cCollider.CP_BULLET; /* Don't use the setCollidePriority mutator, as that
			forces a call to pgame()->buildCollider(); */
            _maxspeed = cCritterBullet.MAXSPEED;
            Speed = cCritterBullet.BULLETSPEED;
            cSpriteSphere bulletsprite = new cSpriteSphere(cCritter.BULLETRADIUS * 2, 6, 6);
            bulletsprite.FillColor = Color.Aqua;
            Sprite = bulletsprite; /* Also sets cSprite._prismdz to cCritter._defaultprismdz, which we
			set to CritterWall.BULLETPRISMDZ above. */
        }

        public bool isKindOf(string str)
        {
            return str == "cCritterBulletKamehameha" || base.IsKindOf(str);
        }

        public override int HitStrength
        {
            get { return _hitstrength; }
            set { _hitstrength = value; }
        }
    }

    class cCritter3Dcharacter : cCritter  
	{ 
		
        public cCritter3Dcharacter( cGame pownergame ) 
            : base( pownergame ) 
		{ 
			addForce( new cForceGravity( 25.0f, new cVector3( 0.0f, -1, 0.00f ))); 
			addForce( new cForceDrag( 20.0f ) );  // default friction strength 0.5 
			Density = 2.0f; 
			MaxSpeed = 30.0f;
            if (pownergame != null) //Just to be safe.
                Sprite = new cSpriteQuake(Framework.models.selectRandomCritter());
            
            // example of setting a specific model
            // setSprite(new cSpriteQuake(ModelsMD2.Knight));
            
            if ( Sprite.IsKindOf( "cSpriteQuake" )) //Don't let the figurines tumble.  
			{ 
				AttitudeToMotionLock = false;   
				Attitude = new cMatrix3( new cVector3( 0.0f, 0.0f, 1.0f ), 
                    new cVector3( 1.0f, 0.0f, 0.0f ), 
                    new cVector3( 0.0f, 1.0f, 0.0f ), Position); 
				/* Orient them so they are facing towards positive Z with heads towards Y. */ 
			} 
			Bounciness = 0.0f; //Not 1.0 means it loses a bit of energy with each bounce.
			setRadius( 1.0f );
            MinTwitchThresholdSpeed = 4.0f; //Means sprite doesn't switch direction unless it's moving fast 
			randomizePosition( new cRealBox3( new cVector3( _movebox.Lox, _movebox.Loy, _movebox.Loz + 4.0f), 
				new cVector3( _movebox.Hix, _movebox.Loy, _movebox.Midz - 1.0f))); 
				/* I put them ahead of the player  */ 
			randomizeVelocity( 0.0f, 30.0f, false ); 

                        
			if ( pownergame != null ) //Then we know we added this to a game so pplayer() is valid 
				addForce( new cForceObjectSeek( Player, 0.5f ));

            int begf = Framework.randomOb.random(0, 171);
            int endf = Framework.randomOb.random(0, 171);

            if (begf > endf)
            {
                int temp = begf;
                begf = endf;
                endf = temp;
            }

			Sprite.setstate( State.Other, begf, endf, StateType.Repeat );


            _wrapflag = cCritter.BOUNCE;

		} 

		
		public override void update( ACView pactiveview, float dt ) 
		{ 
			base.update( pactiveview, dt ); //Always call this first
            rotateAttitude(Tangent.rotationAngle(AttitudeTangent));
			if ( (_outcode & cRealBox3.BOX_HIZ) != 0 ) /* use bitwise AND to check if a flag is set. */ 
				delete_me(); //tell the game to remove yourself if you fall up to the hiz.
        } 

		// do a delete_me if you hit the left end 
	
		public override void die() 
		{ 
			Player.addScore( Value ); 
			base.die(); 
		} 

       public override bool IsKindOf( string str )
        {
            return str == "cCritter3Dcharacter" || base.IsKindOf( str );
        }
	
        public override string RuntimeClass
        {
            get
            {
                return "cCritter3Dcharacter";
            }
        }
	}

    class cCritter3DBoss : cCritterArmedRobot
    {
        public static readonly new float DENSITY = 5.0f;
        Random rand;


        public cCritter3DBoss(cGame pownergame = null) :
            base(pownergame)
        {
            if (pownergame != null)
            {
                rand = new Random();
                BulletClass = new cCritterBulletRubber();
                //Sets the boss sprite
                Sprite = new cSpriteQuake(ModelsMD2.Vegeta);
                setHealth(20);
                WrapFlag = cCritter.CLAMP;  //Prevents boss from going through walls
                Armed = true;   //Allows the character to use bullets
                MaxSpeed = cGame3D.MAXPLAYERSPEED;  //sets max speed
                AbsorberFlag = true;    //Keeps boss from being buffered out
                addForce(new cForceGravity(50.0f)); //gravity
                AttitudeToMotionLock = false;
                //First param determines direction facing (forward/backward)
                Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f), new cVector3(1.0f, 0.0f, 0.0f),
                    new cVector3(0.0f, 1.0f, 0.0f), Position);
                AimToAttitudeLock = true;   //aims in the direction of the attitude
                setMoveBox(_movebox);
                moveTo(new cVector3(_movebox.Midx, _movebox.Loy,
                    _movebox.Midz + 2.0f));
                //Sets the direction the boss is moving to the direction they are facing
                addForce(new cForceObjectSeek(Player, 3.0f));
                _waitshoot = (float)rand.NextDouble();
                setMoveBox(_movebox);
            }
        }

        public override cCritterBullet shoot()
        {
            Framework.snd.play(Sound.LaserFire);
            return base.shoot();
        }

        public override bool collide(cCritter pCritter)
        {
            if (contains(pCritter)) //disk of pcritter is wholly inside my disk 
            {
                pCritter.addHealth(-1);
                pCritter.moveTo(new cVector3(_movebox.Midx, _movebox.Loy + 1.0f,
                    _movebox.Hiz - 3.0f));
                return true;
            }
            else
                return false;
        }
        public override void update(ACView pactiveview, float dt)
        {
            if(Health < 5)
            {
                BulletClass = new cCritterBulletKamehameha();
            }
            base.update(pactiveview, dt); //Always call this first
            rotateAttitude(Tangent.rotationAngle(AttitudeTangent));
            _waitshoot = (float)rand.NextDouble();
        }

        public override void copy(cCritter pcritter)
        {
            base.copy(pcritter);
            if (!pcritter.IsKindOf("cCritter3DBoss"))
                return;
            cCritter3DBoss pcritterplayer = (cCritter3DBoss)(pcritter);
        }
        public override cCritter copy()
        {
            cCritter3DBoss c = new cCritter3DBoss();
            c.copy(this);
            return c;
        }
        public override bool IsKindOf(string str)
        {
            return str == "cCritter3DBoss" || base.IsKindOf(str);
        }
    }
    class cCritter3DCharacterEnemy : cCritterArmedRobot
    {
        private const int begf = 155;
        private const int endf = 160;
        public cCritter3DCharacterEnemy(cGame pownergame) 
            : base( pownergame ) 
        {
            Sprite = new cSpriteQuake(ModelsMD2.Enemy);
            Sprite.setstate(State.Other, begf, endf, StateType.Repeat);
            WrapFlag = cCritter.CLAMP;  //Prevents boss from going through walls
            Armed = false;   //Allows the character to use bullets
            MaxSpeed = cGame3D.MAXPLAYERSPEED - 15.0f;  //sets max speed
            AbsorberFlag = true;    //Keeps boss from being buffered out
            addForce(new cForceGravity(50.0f)); //gravity
            AttitudeToMotionLock = false;
            Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f), new cVector3(1.0f, 0.0f, 0.0f),
               new cVector3(0.0f, 1.0f, 0.0f), Position);
            addForce(new cForceObjectSeek(Player, 3.0f));
            AimToAttitudeLock = true;   //aims in the direction of the attitude
            
        }
        public override void update(ACView pactiveview, float dt)
        {
            base.update(pactiveview, dt); //Always call this first
            rotateAttitude(Tangent.rotationAngle(AttitudeTangent));
            if ((_outcode & cRealBox3.BOX_HIZ) != 0) /* use bitwise AND to check if a flag is set. */
                delete_me(); //tell the game to remove yourself if you fall up to the hiz.
        }

        public override void die()
        {
            Player.addScore(Value);
            base.die();
        }
        public override bool collide(cCritter pother)
        {
            if (pother.IsKindOf("cCritter3DPlayer"))
            {
                //move away from player
                addForce(new cForceClassEvade(0.3f, 0.3f));
                return true;
            }
            return false;
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritter3DCharacterEnemy" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritter3DCharacterEnemy";
            }
        }
    }

    class cCritterWank : cCritter
    {
        private short stateBegF = 0;
        private short stateEndF = 39;
        public cCritterWank(cGame pownergame)
            : base(pownergame)
        {
            //Sets the sprite
            Sprite = new cSpriteQuake(ModelsMD2.Wank);
            WrapFlag = cCritter.CLAMP;  //Prevent from going through walls
            MaxSpeed = cGame3D.MAXPLAYERSPEED;  //sets max speed
            AbsorberFlag = true;    //Keep from being buffered out
            addForce(new cForceGravity(50.0f)); //gravity
            AttitudeToMotionLock = false;
            //First param determines direction facing (forward/backward)
            Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f), new cVector3(1.0f, 0.0f, 0.0f),
                new cVector3(0.0f, 1.0f, 0.0f), Position);
            Sprite.setstate(State.Other, stateBegF, stateEndF, StateType.Repeat);

        }

        public override bool collide(cCritter pother)
        {
            if(pother.IsKindOf("cCritter3DPlayer"))
            {
                return true;
            }
            else if (pother.IsKindOf("cCritter3DPlayerBullet"))
            {
                return true;
            }
            return false;
        }

        public override bool IsKindOf(string str)
        {
            return str =="cCritterWank" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterWank";
            }
        }
    }


    class cCritterGohan : cCritterArmed
    {
        private short stateBegF = 95;
        private short stateEndF = 112;
        public cCritterGohan(cGame pownergame)
            : base(pownergame)
        {
            //Sets the sprite
            Sprite = new cSpriteQuake(ModelsMD2.Gohan);
            setRadius(0.3f);
            WrapFlag = cCritter.CLAMP;  //Prevent from going through walls
            MaxSpeed = cGame3D.MAXPLAYERSPEED;  //sets max speed
            AbsorberFlag = true;    //Keep from being buffered out
            addForce(new cForceGravity(50.0f)); //gravity
            AttitudeToMotionLock = false;
            //First param determines direction facing (forward/backward)
            Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f), new cVector3(1.0f, 0.0f, 0.0f),
                new cVector3(0.0f, 1.0f, 0.0f), Position);
            Sprite.setstate(State.Other, stateBegF, stateEndF, StateType.Repeat);
            //First param determines direction facing (forward/backward)
            Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f), new cVector3(1.0f, 0.0f, 0.0f),
                new cVector3(0.0f, 1.0f, 0.0f), Position);
            addForce(new cForceObjectSeek(Player, 1.0f));
            AimToAttitudeLock = true;   //aims in the direction of the attitude
        }

        public override bool collide(cCritter pother)
        {
            if (pother.IsKindOf("cCritter3DPlayer"))
            {
                return true;
            }
            return false;
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritterGohan" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterGohan";
            }
        }
    }

    class cCritterTreasure : cCritter 
	{   // Try jumping through this hoop
		
		public cCritterTreasure( cGame pownergame ) : 
		base( pownergame ) 
		{ 
			/* The sprites look nice from afar, but bitmap speed is really slow
		when you get close to them, so don't use this. */ 
			cPolygon ppoly = new cPolygon( 24 ); 
			ppoly.Filled = false; 
			ppoly.LineWidthWeight = 0.5f;
			Sprite = ppoly; 
			_collidepriority = cCollider.CP_PLAYER + 1; /* Let this guy call collide on the
			player, as his method is overloaded in a special way. */ 
			rotate( new cSpin( (float) Math.PI / 2.0f, new cVector3(0.0f, 0.0f, 1.0f) )); /* Trial and error shows this
			rotation works to make it face the z diretion. */ 
			setRadius( cGame3D.TREASURERADIUS ); 
			FixedFlag = true;
            //setMoveBox(64.0f, 16.0f, 64.0f);
            moveTo( new cVector3( _movebox.Midx, _movebox.Midy - 2.0f, 
				_movebox.Loz - 1.5f * cGame3D.TREASURERADIUS )); 
		} 

		
		public override bool collide( cCritter pcritter ) 
		{ 
			if ( contains( pcritter )) //disk of pcritter is wholly inside my disk 
			{
                Framework.snd.play(Sound.Clap); 
				pcritter.addScore( 100 ); 
				pcritter.addHealth( 1 ); 
				pcritter.moveTo( new cVector3( _movebox.Midx, _movebox.Loy + 1.0f,
                    _movebox.Hiz - 3.0f )); 
				return true; 
			} 
			else 
				return false; 
		} 

		//Checks if pcritter inside.
	
		public override int collidesWith( cCritter pothercritter ) 
		{ 
			if ( pothercritter.IsKindOf( "cCritter3DPlayer" )) 
				return cCollider.COLLIDEASCALLER; 
			else 
				return cCollider.DONTCOLLIDE; 
		} 

		/* Only collide
			with cCritter3DPlayer. */ 

       public override bool IsKindOf( string str )
        {
            return str == "cCritterTreasure" || base.IsKindOf( str );
        }
	
        public override string RuntimeClass
        {
            get
            {
                return "cCritterTreasure";
            }
        }
	}

    class cCritterDragonball : cCritter
    {
        //hold a reference to the game
        private cGame3D _game;
        // Try jumping through this hoop
        public cCritterDragonball(cGame pownergame) :
        base(pownergame)
        {
            _game = pownergame as cGame3D;
            /* The sprites look nice from afar, but bitmap speed is really slow
		when you get close to them, so don't use this. */
            cSpriteSphere sphere = new cSpriteSphere(20, 16, 16);
            sphere.FillColor = Color.Orange;
            sphere.Filled = true;
            sphere.LineWidthWeight = 0.5f;
            Sprite = sphere;
            //rotate(new cSpin((float)Math.PI / 2.0f, new cVector3(0.0f, 0.0f, 1.0f))); 
            setRadius(0.5f);
            this.clearForcelist();
            addForce(new cForceDrag(100.0f));
        }

        /* Only collide
			with cCritter3DPlayer. */

        public override bool IsKindOf(string str)
        {
            return str == "cCritterDragonball" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterDragonball";
            }
        }
    }

    //======================cGame3D========================== 

    class cGame3D : cGame 
	{ 
		public static readonly float TREASURERADIUS = 1.2f; 
		public static readonly float WALLTHICKNESS = 0.5f; 
		public static readonly float PLAYERRADIUS = 0.2f; 
		public static readonly float MAXPLAYERSPEED = 30.0f;
        public static readonly float BORDER_XZ = 50.0f;//length and width of rooms
        public static readonly float BORDER_Y = 16.0f;//height of rooms
		private bool doorcollision;
        private bool wentThrough = false;
        private float startNewRoom;
        private int roomNumber = 1;//keeps track of the room that the player is currently in
        private cCritterMovingWall movingWall;
        private bool shouldMoveWall;
    

        public cGame3D() 
		{
			doorcollision = false;
			_menuflags &= ~ cGame.MENU_BOUNCEWRAP; 
			_menuflags |= cGame.MENU_HOPPER; //Turn on hopper listener option.
			_spritetype = cGame.ST_MESHSKIN; 
			setBorder( BORDER_XZ, BORDER_Y, BORDER_XZ ); // size of the world
			cRealBox3 skeleton = new cRealBox3();
            skeleton.copy(_border);
			setSkyBox( skeleton );
            shouldMoveWall = false;
            
		/* In this world the coordinates are screwed up to match the screwed up
		listener that I use.  I should fix the listener and the coords.
		Meanwhile...
		I am flying into the screen from HIZ towards LOZ, and
		LOX below and HIX above and
		LOY on the right and HIY on the left. */ 
			SkyBox.setSideTexture(cRealBox3.HIY, BitmapRes.Sky); //ceiling
			SkyBox.setSideTexture( cRealBox3.LOY, BitmapRes.Concrete ); //floor  
			SkyBox.setSideTexture( cRealBox3.LOX, BitmapRes.Dragonball_bg1_90r ); //Left Wall - flip 90r
			SkyBox.setSideTexture( cRealBox3.HIX, BitmapRes.Dragonball_bg1_90r ); //Right Wall -flip 90l
			SkyBox.setSideTexture( cRealBox3.HIZ, BitmapRes.Dragonball_bg1 ); //Front Wall  
			SkyBox.setSideTexture( cRealBox3.LOZ, BitmapRes.Dragonball_bg1 ); //Back Wall  

            WrapFlag = cCritter.BOUNCE; 
			_seedcount = 0; 
			setPlayer( new cCritter3DPlayer( this )); 
		
			/* In this world the x and y go left and up respectively, while z comes out of the screen.
		A wall views its "thickness" as in the y direction, which is up here, and its
		"height" as in the z direction, which is into the screen. */ 
			//First draw a wall with dy height resting on the bottom of the world.
			float zpos = 0.0f; /* Point on the z axis where we set down the wall.  0 would be center,
			halfway down the hall, but we can offset it if we like. */ 
			float height = _border.YSize; 
			float ycenter = -_border.YRadius + height / 2.0f; 
			float wallthickness = cGame3D.WALLTHICKNESS;
            cCritterWall pwall = new cCritterWall( 
				new cVector3( _border.Midx + 10.0f, ycenter, zpos -12 ), 
				new cVector3( _border.Hix, ycenter, zpos - 12 ), 
				height, //thickness param for wall's dy which goes perpendicular to the 
					//baseline established by the frist two args, up the screen 
				wallthickness, //height argument for this wall's dz  goes into the screen 
				this );
			cSpriteTextureBox pspritebox = 
				new cSpriteTextureBox( pwall.Skeleton, BitmapRes.Wall3, 16 ); //Sets all sides 
				/* We'll tile our sprites three times along the long sides, and on the
			short ends, we'll only tile them once, so we reset these two.*/
          pwall.Sprite = pspritebox;

            cCritterWall pNEWall = new cCritterWall(
                new cVector3(_border.Midx + 10.0f, _border.Midy, zpos - 17),
                new cVector3(_border.Midx + 10.0f, _border.Midy, _border.Loz),
                wallthickness,
                height,
                this);
            cSpriteTextureBox pNEWallSprite = new cSpriteTextureBox(pNEWall.Skeleton, BitmapRes.Wall3, 16);
            pNEWall.Sprite = pNEWallSprite;

            cCritterDoor pdwall = new cCritterDoor( 
				new cVector3( _border.Midx, _border.Loy, _border.Loz-0.9f ), 
				new cVector3( _border.Midx, _border.Midy, _border.Loz-0.9f ), 
				5.0f, 2, this ); 
			cSpriteTextureBox pspritedoor = 
				new cSpriteTextureBox( pdwall.Skeleton, BitmapRes.Door ); 
			pdwall.Sprite = pspritedoor;

            cCritterWank wank = new cCritterWank(this);
            wank.moveTo(new cVector3(_border.Hix - 4.0f, 0.0f, _border.Loz + 4.0f));

            cCritter3DCharacterEnemy enemy_1 = new cCritter3DCharacterEnemy(this);
            enemy_1.moveTo(new cVector3(_border.Hix - 3.0f, _border.Midy + 3.0f, _border.Loz + 10.0f));

            cCritterDragonball dball1 = new cCritterDragonball(this);
            dball1.moveTo(new cVector3(_border.Midx, Border.Loy, _border.Midz));

            cCritterDragonball dball2 = new cCritterDragonball(this);
            dball2.moveTo(new cVector3(_border.Hix - 6, Border.Loy, _border.Loz + 6));

        }
        

        public void setRoom1( )
        {
            Biota.purgeCritters("cCritterWall");
            Biota.purgeCritters("cCritter3Dcharacter");
            Biota.purgeCritters("cCritterWank");
            Biota.purgeCritters("cCritterGohan");
            setBorder(BORDER_XZ, BORDER_Y, BORDER_XZ);
            cRealBox3 skeleton = new cRealBox3();
            skeleton.copy( _border );
	        setSkyBox(skeleton);
	        SkyBox.setAllSidesTexture( BitmapRes.Dragonball_bg3, 0 );
            SkyBox.setSideTexture(cRealBox3.LOX, BitmapRes.Dragonball_bg3_90r);
            SkyBox.setSideTexture(cRealBox3.HIX, BitmapRes.Dragonball_bg3_90r);
            SkyBox.setSideTexture( cRealBox3.LOY, BitmapRes.Concrete );
	        SkyBox.setSideSolidColor( cRealBox3.HIY, Color.Blue );
	        _seedcount = 0;
	        Player.setMoveBox( new cRealBox3(BORDER_XZ, BORDER_Y, BORDER_XZ) );
            Player.MaxSpeed = 50;//reduce player max speed to account for smaller room
            Player.moveTo(new cVector3(_border.Midx, _border.Midy, _border.Hiz));
            float zpos = 0.0f; /* Point on the z axis where we set down the wall.  0 would be center,
			halfway down the hall, but we can offset it if we like. */
            float height = 0.1f * _border.YSize;
            float ycenter = -_border.YRadius + height / 2.0f;
            wentThrough = true;
            startNewRoom = Age;
            cCritterDoor pdwall = new cCritterDoor(
                new cVector3(_border.Lox, _border.Loy, _border.Midz),
                new cVector3(_border.Lox, _border.Midy - 3, _border.Midz),
                0.1f, 3, this);
            cSpriteTextureBox pspritedoor =
                new cSpriteTextureBox(pdwall.Skeleton, BitmapRes.Door);
            pdwall.Sprite = pspritedoor;

            //Draw the ramp
            float width = 10.0f;
            cCritterWall pwall = new cCritterWall(
                new cVector3(_border.Hix - width / 2.0f, _border.Loy -1, _border.Hiz - 2.0f),
                new cVector3(_border.Hix - width / 2.0f, _border.Loy + 6, zpos),
                width,
                cGame3D.WALLTHICKNESS,
                this);
            cSpriteTextureBox stb = new cSpriteTextureBox(pwall.Skeleton,
                BitmapRes.Wood2, 2);
            pwall.Sprite = stb;

            //draw a platform at the end of the ramp
            cCritterWall pPlatform = new cCritterWall(
                new cVector3(_border.Hix-_border.XSize/2.0f, _border.Loy+6, zpos),
                new cVector3(_border.Hix-_border.XSize/2.0f, _border.Loy+6, _border.Loz),
                _border.XSize,
                cGame3D.WALLTHICKNESS,
                this);
            cSpriteTextureBox pPlatformSprite = new cSpriteTextureBox(pPlatform.Skeleton, BitmapRes.Wall3, 16);
            pPlatform.Sprite = pPlatformSprite;

            //draw a wall on the lox of the ramp
            cCritterWall pRampWall = new cCritterWall(
                new cVector3(_border.Hix - width, _border.Midy, _border.Hiz - 3),
                new cVector3(_border.Hix - width, _border.Midy, zpos),
                cGame3D.WALLTHICKNESS,
                _border.YSize,
                this);
            cSpriteTextureBox pRampWallSprite = new cSpriteTextureBox(pRampWall.Skeleton, BitmapRes.Wall3, 16);
            pRampWall.Sprite = pRampWallSprite;

            //close off the upstairs room
            cCritterWall pUpstairsWall = new cCritterWall(
                new cVector3(_border.Hix - width, _border.Midy + 0.35f*_border.YSize, zpos),
                new cVector3(_border.Lox, _border.Midy + 0.35f*_border.YSize, zpos),
                _border.YSize,
                cGame3D.WALLTHICKNESS,
                this);
            cSpriteTextureBox pUpstairsWallSprite = new cSpriteTextureBox(pUpstairsWall.Skeleton, BitmapRes.Wall3, 16);
            pUpstairsWall.Sprite = pUpstairsWallSprite;
            cCritter3DCharacterEnemy enemy = new cCritter3DCharacterEnemy(this);
            enemy.moveTo(new cVector3(0.0f, -4.0f, 0.0f));
            cCritter3DCharacterEnemy enemy2= new cCritter3DCharacterEnemy(this);
            enemy2.moveTo(new cVector3(_border.Midx, _border.Loz + 4, _border.Hiy));


            //Insert dragonballs
            cCritterDragonball dball3 = new cCritterDragonball(this);
            dball3.moveTo(new cVector3(_border.Midx, Border.Loy, _border.Midz));

            cCritterDragonball dball4 = new cCritterDragonball(this);
            dball4.moveTo(new cVector3(_border.Midx, Border.Midy + 3, _border.Loz + 4));

            cCritterDragonball dball5 = new cCritterDragonball(this);
            dball5.moveTo(new cVector3(_border.Hix - 2, Border.Loy, _border.Midz - 2));

        }
        public void setRoom2()
        {
            Biota.purgeCritters("cCritterWall");
            Biota.purgeCritters("cCritter3Dcharacter");
            Biota.purgeCritters("cCritter3DCharacterEnemy");
            setBorder(BORDER_XZ, BORDER_Y, BORDER_XZ);
            cRealBox3 skeleton = new cRealBox3();
            skeleton.copy(_border);
            setSkyBox(skeleton);
            SkyBox.setAllSidesTexture(BitmapRes.BossRoom, 0);
            SkyBox.setSideTexture(cRealBox3.LOX, BitmapRes.BossRoom_90r);
            SkyBox.setSideTexture(cRealBox3.HIX, BitmapRes.BossRoom_90r);
            SkyBox.setSideTexture(cRealBox3.LOY, BitmapRes.Concrete);
            SkyBox.setSideSolidColor(cRealBox3.HIY, Color.Blue);
            _seedcount = 0;
            Player.setMoveBox(new cRealBox3(BORDER_XZ, BORDER_Y, BORDER_XZ));
            Player.MaxSpeed = 30;//reduce player max speed to account for smaller room
            Player.moveTo(new cVector3(_border.Midx, _border.Midy, _border.Hiz));
            float zpos = 0.0f; /* Point on the z axis where we set down the wall.  0 would be center,
			halfway down the hall, but we can offset it if we like. */
            float height = 0.1f * _border.YSize;
            float ycenter = -_border.YRadius + height / 2.0f;
            float wallthickness = cGame3D.WALLTHICKNESS;
            cCritterWall pwall = new cCritterWall(
                new cVector3(_border.Lox, ycenter, zpos),
                new cVector3(_border.Hix, ycenter, zpos),
                _border.YSize - 4, //thickness param for wall's dy which goes perpendicular to the 
                        //baseline established by the frist two args, up the screen 
                wallthickness, //height argument for this wall's dz  goes into the screen 
                this);
            cSpriteTextureBox pspritebox =
                new cSpriteTextureBox(pwall.Skeleton, BitmapRes.Wall3, 16); //Sets all sides 
            /* We'll tile our sprites three times along the long sides, and on the
        short ends, we'll only tile them once, so we reset these two. */
            pwall.Sprite = pspritebox;
            wentThrough = true;
            startNewRoom = Age;
            movingWall = new cCritterMovingWall(
                new cVector3(_border.Midx -2, _border.Midy, _border.Loz + 10.0f),
                new cVector3(_border.Midx -2, _border.Midy, _border.Loz + 2.0f),
                10.0f, 1, this);
            cSpriteTextureBox pspritedoor =
                new cSpriteTextureBox(movingWall.Skeleton, BitmapRes.Door);
            movingWall.Sprite = pspritedoor;
            shouldMoveWall = true;

            //spawn two bosses in final room
            cCritter3DBoss boss1 = new cCritter3DBoss(this);
            boss1.moveTo(new cVector3(_border.Hiz - 3.0f, _border.Loy, _border.Hix - 3.0f));
            cCritter3DBoss boss2 = new cCritter3DBoss(this);
            boss2.moveTo(new cVector3(_border.Loz + 3.0f, _border.Loy, _border.Lox + 3.0f));

            cCritterDragonball dball6 = new cCritterDragonball(this);
            dball6.moveTo(new cVector3(_border.Lox, Border.Loy, _border.Hiz-3.0f));

            cCritterDragonball dball7 = new cCritterDragonball(this);
            dball7.moveTo(new cVector3(_border.Hix - 2, Border.Loy, _border.Loz + 2));

        }
		public override void seedCritters() 
		{
			Biota.purgeCritters( "cCritterBullet" ); 
			Biota.purgeCritters( "cCritter3Dcharacter" );
            for (int i = 0; i < _seedcount; i++)
                //new cCritter3DBoss(this);
				//new cCritter3Dcharacter( this );
            Player.moveTo(new cVector3(0.0f, Border.Loy, Border.Hiz - 3.0f)); 
				/* We start at hiz and move towards	loz */ 
		} 

		
		public void setdoorcollision( ) { doorcollision = true; } 
		
		public override ACView View 
		{
            set
            {
                base.View = value; //You MUST call the base class method here.
                value.setUseBackground(ACView.FULL_BACKGROUND); /* The background type can be
			    ACView.NO_BACKGROUND, ACView.SIMPLIFIED_BACKGROUND, or 
			    ACView.FULL_BACKGROUND, which often means: nothing, lines, or
			    planes&bitmaps, depending on how the skybox is defined. */
                value.pviewpointcritter().Listener = new cListenerViewerRide();
            }
		} 

		
		public override cCritterViewer Viewpoint 
		{ 
            set
            {
			    if ( value.Listener.RuntimeClass == "cListenerViewerRide" ) 
			    { 
				    value.setViewpoint( new cVector3( 0.0f, 0.3f, -1.0f ), _border.Center); 
					//Always make some setViewpoint call simply to put in a default zoom.
				    value.zoom( 0.2f ); //Wideangle 
				    cListenerViewerRide prider = ( cListenerViewerRide )( value.Listener); 
				    prider.Offset = (new cVector3( -1.5f, 0.0f, 2.5f)); /* This offset is in the coordinate
				    system of the player, where the negative X axis is the negative of the
				    player's tangent direction, which means stand right behind the player. */ 
			    } 
			    else //Not riding the player.
			    { 
				    value.zoom( 1.0f ); 
				    /* The two args to setViewpoint are (directiontoviewer, lookatpoint).
				    Note that directiontoviewer points FROM the origin TOWARDS the viewer. */ 
				    value.setViewpoint( new cVector3( 0.0f, 0.3f, 1.0f ), _border.Center); 
			    }
            }
		} 

		/* Move over to be above the
			lower left corner where the player is.  In 3D, use a low viewpoint low looking up. */ 
	
		public override void adjustGameParameters() 
		{
		// (1) End the game if the player is dead 
			if ( (Health == 0) && !_gameover ) //Player's been killed and game's not over.
			{ 
				_gameover = true; 
				Player.addScore( _scorecorrection ); // So user can reach _maxscore  
                Framework.snd.play(Sound.Hallelujah);
                return ; 
			} 

            if(cCritter3DPlayer.numDragBallsCollected >= 7)
            {
                _gameover = true;
                Application.Exit();
                MessageBox.Show("You have won the game!!");
                return;
            }
            // (2) Also don't let the the model count diminish.
            //(need to recheck propcount in case we just called seedCritters).
            int modelcount = Biota.count( "cCritter3Dcharacter" ); 
			int modelstoadd = _seedcount - modelcount; 
			for ( int i = 0; i < modelstoadd; i++) 
				new cCritter3Dcharacter( this ); 
		// (3) Maybe check some other conditions.

            if (wentThrough && (Age - startNewRoom) > 2.0f)
            {
                wentThrough = false;
            }

            if (doorcollision == true)
            {
                if (roomNumber == 1 && cCritter3DPlayer.numDragBallsCollected >=2)
                {
                    setRoom1();
                    roomNumber = 2;
                }
                else if (roomNumber == 2 && cCritter3DPlayer.numDragBallsCollected >=5)
                {
                    setRoom2();
                    shouldMoveWall = true;
                }
                //else if (roomNumber == 3 && cCritter3DPlayer.numDragBallsCollected >=7)
                //{
                //    MessageBox.Show("You won the game!");
                //}
                else
                {
                    MessageBox.Show("This door seems to be locked.");
                    Player.moveTo(Player.Position.sub(new cVector3(2.0f, 2.0f, 2.0f)));
                }
           
                doorcollision = false;
            }
            if(shouldMoveWall)
            {
                movingWall.moveWall();
            }
		} 
		
	} 
	
}