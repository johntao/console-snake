# A text-based snake game

Intro
- A text-based minimal snake game
- I'm trying NOT to find out if someone has already done it

Design proposal
- A config file to control the game parameters as many as possible
  - the config file should be auto-generated if not exist
  - the config file should be in TOML format (cuz why not?!)
- Game parameters
  - Gameplay
    - starting Length
    - can hit wall
    - (opt) add temporary objective
    - useSpeed
    - useLevel
  - GameplaySpeed
    - Starting Speed
    - use levels to boost speed
    - whether arrow keys can boost speed
    - (opt) speed boost for temporary objective
  - GameplayLevel
    - LevelSpeedDefinition
    - Threshold
    - DefaultLevel
  - Visual
    - show/ hide dashboard
    - show/ hide border
  - VisualMap
    - size of map (must be square)
    - map legend (game unit and tile definition)
- Game rules
  - game start on the first key stroke
  - game level up by eating items
  - snake grows up by eating items
  - the only score is the length of the snake
    - not interested in calculating complex scores
  - dashboard fields: Level, Speed, Length, Time, HighScore
  - definite level and speed booster
  - game end on win/ loss condition
  - user can start a new game after defeated
- Code implementation
  - the map should be a simple 2d array
  - code should be as simple as possible
  - not interested in TDD or DDD
  - thread usage as less as possible
    - should be easy to extend to multi-thread
  - should use dependency injection
  - configuration should be strongly typed
  - use latest language features if possible
- Code deployment
  - no test coverage
  - no CI/ CD
  - no docker
  - no cloud