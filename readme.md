<style>
   ol { padding-left: 2rem }
</style>
# A text-based snake game

Intro
- A text-based minimal snake game
- I'm trying NOT to find out if someone has already done it

Design proposal
1. A config file to control the game parameters as many as possible
   1. the config file should be auto-generated if not exist
   2. the config file should be in TOML format (cuz why not?!)
2. Game parameters
   1. Gameplay
      1. starting Length
      2. can hit wall
      3. (opt) add temporary objective
      4. useSpeed
      5. useLevel
   2. GameplaySpeed
      1. Starting Speed
      2. use levels to boost speed
      3. whether arrow keys can boost speed
      4. (opt) speed boost for temporary objective
   3. GameplayLevel
      1. LevelSpeedDefinition
      2. Threshold
      3. DefaultLevel
   4. Visual
      1. show/ hide dashboard
      2. show/ hide border
   5. VisualMap
      1. size of map (must be square)
      2. map legend (game unit and tile definition)
3. Game rules
   1. game start on the first key stroke
   2. game level up by eating items
   3. snake grows up by eating items
   4. the only score is the length of the snake
      1. NO PLANS to implement complex score
   5. dashboard fields: Level, Speed, Length, Time, HighScore
   6. definite level and speed booster
   7. game end on win/ loss condition
   8. user can start a new game after defeated
   9. add win/ loss indicator
4. Code implementation
   1. the map should be a simple 2d array
   2. code should be as simple as possible
   3. NO PLANS to do TDD or DDD
   4. thread usage as less as possible
      1. should be easy to extend to multi-thread
   5. should use dependency injection
   6. configuration should be strongly typed
   7. use latest language features if possible
   8. NO PLANS to improve the text rendering experience
      1. border should be rendered contiguously
      2. handle half-width, full-width char gracefully
      3. support colors
5. Code deployment
   1. no test coverage
   2. no CI/ CD
   3. no docker
   4. no cloud