
using System;
using System.Collections.Generic;

namespace OpenCSharpFlow
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            VikingWizard fighter1 = new VikingWizard();
            VikingWizard fighter2 = new VikingWizard();

            var battle = new combat.implementation.handler.BattleRoyale();
            battle.add(fighter1);
            battle.add(fighter2);

            while (!battle.fightOver)
            {
                Console.WriteLine("The Battle Rages On...");
                battle.next();
            }
        }

        class VikingWizard : combat.implementation.agent.Brawler
        {
            public VikingWizard()
            {
                //ATTACKS
                combat.StatBlock firebolt = new combat.StatBlock();
                firebolt.set(1, 10);
                addAttack(firebolt);
                combat.StatBlock hellsaber = new combat.StatBlock();
                hellsaber.set(0, 2, 3, 3);
                addAttack(hellsaber);

                //ATTACKS
                combat.StatBlock divine_benediction = new combat.StatBlock();
                firebolt.set(1, 2, 5, 0);
                addDefense(divine_benediction);
                combat.StatBlock shield_wall = new combat.StatBlock();
                shield_wall.set(0, 8);
                addDefense(hellsaber);
            }
        }
    }
}

namespace combat
{
    
    class StatBlock
    {
        /*
         * StatBlock: Represents a single attack, defense, or damage. Either constant or dice.
         */
        public static int numberOfStats = 10; //How many stats will be present.
        public bool allowNegativeStats = false; //Whether negative stats are allowed. Effects get() and operators. If false, negative numbers go to 0.

        //Optional values. These have no effect on the operation of the StatBlock. They only store contextual information.
        public string? name;
        public CombatAgent? user;

        private int[] numberOfDice; //Holds how many dice, if any, will be rolled.
        private int[] numberOfDiceSides; //Holds how many sides each dice has - if any.
        private int[] offset; //Holds constant offsets.

        public StatBlock()
        {
            //Set up arrays
            numberOfDice      = new int[numberOfStats];
            numberOfDiceSides = new int[numberOfStats];
            offset            = new int[numberOfStats];

            //Initialize arrays
            for (int i = 0; i < numberOfStats; i++)
            {
                numberOfDice[i] = numberOfDiceSides[i] = offset[i] = 0;
            }
        }

        public void set(int statNumber, int _offset)
        {
            /*
             * Just sets a constant. No dice will be rolled.
             */
            offset[statNumber] = _offset;
        }

        public void set(int statNumber, int _numberOfDice , int _numberOfDiceSides, int _offset)
        {
            /*
             * Sets dice and constant.
             */
            numberOfDice[statNumber] = _numberOfDice;
            numberOfDiceSides[statNumber] = _numberOfDiceSides;
            offset[statNumber] = _offset;
        }

        public int get(int statNumber)
        {
            /*
             * Rolls dice and returns the result.
             */

            var rand = new Random();
            int total = 0;

            for (int i = 0; i < numberOfDice[statNumber]; i++)
            {
                int newNum = rand.Next(numberOfDiceSides[statNumber] + 1);
                total += newNum;
            }

            total+=offset[statNumber];
            if (total < 0 && !allowNegativeStats)
            {
                return 0;
            }
            return total;
        }

        public int getNumberOfDice(int statNumber)
        {
            return numberOfDice[statNumber];
        }

        public int getNumberOfDiceSides(int statNumber)
        {
            return numberOfDiceSides[statNumber];
        }

        public int getOffset(int statNumber)
        {
            return offset[statNumber];
        }

        public int accumulate()
        {
            /*
             * Adds up total of all stats.
             */
            int total = 0;
            for (int i = 0; i < numberOfStats; i++)
            {
                total += get(i);
            }
            return total;
        }

        //Operators
        //These are provided for convienence. Evaluations are performed and the result stored in offset.

        public static StatBlock operator - (StatBlock s1, StatBlock s2)
        {
            StatBlock newStatBlock = new StatBlock();

            for (int i = 0; i < numberOfStats; i++)
            {
                newStatBlock.set(i, s1.get(i) - s2.get(i));
            }

            return newStatBlock;
        }

        public static StatBlock operator +(StatBlock s1, StatBlock s2)
        {
            StatBlock newStatBlock = new StatBlock();

            for (int i = 0; i < numberOfStats; i++)
            {
                newStatBlock.set(i, s1.get(i) + s2.get(i));
            }

            return newStatBlock;
        }

        //Debug
        public void info()
        {
            for (int i = 0; i < numberOfStats; i++)
            {
                Console.WriteLine(String.Format("{0}: {1}d{2}+{3} => {4}", i, getNumberOfDice(i), getNumberOfDiceSides(i), getOffset(i),get(i)));
            }
        }
    }

    abstract class CombatAgent
    {
        public int id;
        public bool dead = false;

        //NOTE: Do I really want this?
        public int HP = 30; //This is only declared here for conveinence. A negative or zero value will not on it's own kill the CombatAgent. 

        //Optional values that have no impact on performance.
        public string? name;

        public abstract StatBlock getDefense(StatBlock attack);
        public abstract (List<CombatAgent>, StatBlock) getAttack(CombatHandler handler); //Should return a list of targets of the attack.
        public abstract void takeDamage(StatBlock damage);

        public CombatAgent()
        {
            var rand = new Random();
            id = rand.Next();
        }
    }

    abstract class CombatHandler
    {
        public List<CombatAgent> fighters;
        public bool fightOver = false;

        private cli.CombatDisplay? display;
        private int currentFighter; //Index of current fighter in fighter queue.
        private bool fightHasBegun = false;

        public CombatHandler(cli.CombatDisplay? _display)
        {
            fighters = new List<CombatAgent>();
            currentFighter = 0;
            display = _display;
        }

        public void next()
        {
            /*
             * Allows the next combatant to attack.
             */
            if (!fightHasBegun)
            {
                fightHasBegun = true;
                if (!(display is null))
                {
                    display.battleStart();
                }
            }

            currentFighter += 1;
            if (currentFighter > fighters.Count-1)
            {
                currentFighter = 0;
            }

            CombatAgent fighter = fighters[currentFighter];
            (List<CombatAgent>, StatBlock) action = fighter.getAttack(this);
            List<CombatAgent> attacked_agents = action.Item1;
            StatBlock attack = action.Item2;

            if (!(display is null))
            {
                display.turnStart(fighter);
                display.attack(fighter, attack, attacked_agents);
            }

            foreach (CombatAgent agent in attacked_agents)
            {
                StatBlock defense = agent.getDefense(attack);
                StatBlock damage = attack - defense;
                
                if (!(display is null))
                {
                    display.defend(agent, defense, attack);
                    display.damage(agent, damage);
                }
                agent.takeDamage(damage);

                if (agent.dead)
                {
                    if (!(display is null))
                    {
                        display.death(agent);
                    }
                    agentDead(agent);
                }
            }
        }

        public void add(CombatAgent agent)
        {
            /*
             * Adds a new agent to battle.
             */
            fighters.Add(agent);
        }

        abstract public void agentDead(CombatAgent agent); //Called when an agent has died. Should also set whether or not the fight is over.
    }

    /*
     * Various implementations of the abstract classes.
     * These could be considered actual objects to use in a game, or merely examples of possible usage.
     */
    namespace implementation
    {
        namespace agent
        {
            abstract class NPC: CombatAgent
            {
                /*
                 * The base class for all "basic" NPCs. These NPCs have a list of attacks and defenses that are predefined, and do not change in the middle of battle.
                 */
                protected List<StatBlock> attacks;
                protected List<StatBlock> defenses;
                
                public NPC()
                {
                    attacks = new List<StatBlock>();
                    defenses = new List<StatBlock>();
                }

                public void addAttack(StatBlock attack)
                {
                    attacks.Add(attack);
                }

                public void addDefense(StatBlock defense)
                {
                    defenses.Add(defense);
                }
            }

            class Brawler : NPC
            {
                /*
                 * The Brawler has no alliance system. Instead, it merely chooses an enemy at random and attacks.
                 */

                
                public override (List<CombatAgent>, StatBlock) getAttack(CombatHandler handler)
                {
                    Random rnd = new Random();

                    Console.WriteLine(String.Format("It's {0}'s turn to attack!", id));

                    //Random Attack
                    int attack_index = rnd.Next(attacks.Count);
                    StatBlock attack = attacks[attack_index];

                    //Random Defender
                    //TODO: Stop him from hitting himself.
                    int defender_index = rnd.Next(handler.fighters.Count);
                    CombatAgent defender = handler.fighters[defender_index];
                    List<CombatAgent> defenders = new List<CombatAgent>();
                    defenders.Add(defender);

                    Console.WriteLine(String.Format("{0} uses {1} against {2}", id, attack_index, defender.id));
                    return (defenders, attack);
                }

                public override StatBlock getDefense(StatBlock attack)
                {
                    Random rnd = new Random();

                    //Random Defense
                    int defense_index = rnd.Next(defenses.Count);
                    StatBlock defense = attacks[defense_index];

                    Console.WriteLine(String.Format("{0} defends with {1}", id, defense_index));

                    return defense;
                }

                public override void takeDamage(StatBlock damage)
                {
                    
                    HP -= damage.accumulate();
                    Console.WriteLine(String.Format("{0} takes {1} points of damage, leaving him with {2} HP", id, damage.accumulate(), HP));
                    if (HP < 0)
                    {
                        dead = true;
                    }
                }
            }
        }
    
        namespace handler
        {
            class BattleRoyale : CombatHandler
            {
                /*
                 * Combat ends when only one man is left standing.
                 */

                public override void agentDead(CombatAgent agent)
                {
                    Console.WriteLine(String.Format("{0} has perished!", agent.id));

                    //Remove the dead.
                    int deadFighterIndex = 0;
                    for (int i = 0; i < fighters.Count; i++)
                    {
                        if (fighters[i].id == agent.id)
                        {
                            deadFighterIndex = i;
                        }
                    }
                    fighters.RemoveAt(deadFighterIndex);

                    if (fighters.Count <= 1)
                    {
                        fightOver = true;
                    }
                }
            }
        }
    }
}

namespace cli
{
    /*
     * Tools for creating a CLI (Command Line Interface) for your games.
     */

    class StatBlockUtilities
    {
        /*
         * Utilities for displaying information about StatBlocks.
         */

        static public string valueRepresentation(combat.StatBlock statBlock, int index)
        {
            int offset = statBlock.getOffset(index);
            int numberOfDice = statBlock.getNumberOfDice(index);

            if (numberOfDice == 0)
            {
                return $"{offset}";
            }

            int numberOfDiceSides = statBlock.getNumberOfDice(index);

            return $"{numberOfDice}d{numberOfDiceSides}+{offset}";
        }
    }

    abstract class InformationDisplay
    {
        /*
         * A base class for all classes needing to output some information in a string basis. 
         * 
         */
        public bool writeToConsole = true; //If true, immediately do a Console.WriteLine() of new messages. Otherwise, store to a buffer. Storing to a buffer might be useful if you want to migrate to a GUI.
        private Queue<string> messageBuffer; //If writeToConsole is false, messages will be stored here. 
        public string? prefix; //An optional value for prepending a prefix to a message.

        public void print(string toPrint)
        {
            /*
             * Handle a new string.
             */

            string message;
            if (prefix is string)
            {
                message = $"[{prefix}] {toPrint}";
            }
            else
            {
                message = toPrint;
            }

            if (writeToConsole)
            {
                Console.WriteLine(message);
            }
            else
            {
                messageBuffer.Enqueue(message);
            }
        }
    
        public string nextMessage()
        {
            return messageBuffer.Dequeue();
        }
    }

    abstract class CombatDisplay : InformationDisplay
    {
        public abstract void battleStart(); //Called at the beginning of the battle.
        public abstract void turnStart(combat.CombatAgent agent); //Called at the beginning of agent's turn.
        public abstract void attack(combat.CombatAgent attacker, combat.StatBlock attack, List<combat.CombatAgent> targets); //Called when attacker uses attack against targets.
        public abstract void defend(combat.CombatAgent defender, combat.StatBlock defense, combat.StatBlock attack); //Called when defender uses defense against attack.
        public abstract void damage(combat.CombatAgent defender, combat.StatBlock damage); //Called when defender is damaged.
        public abstract void death(combat.CombatAgent defender); //Called when defender has died :(
        public abstract void battleEnd(); //Called at the end of combat.
    }
}