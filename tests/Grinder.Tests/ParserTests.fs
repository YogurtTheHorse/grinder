module Grinder.ParserTests

open System
open Xunit
open Grinder.Commands.Parser
open FParsec
    
[<Theory>]
[<InlineData("1 minutes", 6u)>]
[<InlineData("4 min", 6u)>]
[<InlineData("12 mins", 12u)>]
[<InlineData("31 min", 31u)>]
let ``pminutes parses minutes`` (command: string, expected: uint32) =
  match run UsernameCommands.pminutes command with
  | Success(res, _, _) ->
    Assert.Equal(expected, res)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
        
[<Theory>]
[<InlineData("1 day", 1u)>]
[<InlineData("4 days", 4u)>]
let ``pdays parses days`` (command: string, expected: uint32) =
  match run UsernameCommands.pdays command with
  | Success(res, _, _) ->
    Assert.Equal(expected, res)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
        
[<Theory>]
[<InlineData("1 month", 1u)>]
[<InlineData("4 months", 4u)>]
let ``pmonths parses months`` (command: string, expected: uint32) =
  match run UsernameCommands.pmonths command with
  | Success(res, _, _) ->
    Assert.Equal(expected, res)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
        
[<Theory>]
[<InlineData("@name", "@name")>]
[<InlineData("@name  123", "@name")>]
[<InlineData("@name  @other", "@name")>]
[<InlineData("@name@other", "@name")>]
let ``pusername parses username`` (command: string, expected: string) =
  match run UsernameCommands.pusername command with
  | Success(res, _, _) ->
    Assert.Equal(expected, res)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
        
[<Theory>]
[<InlineData("@bot", "@bot")>]
[<InlineData(" @bot ", "@bot")>]
[<InlineData("\r\n@bot ", "@bot")>]
let ``pbotUsername parses bot username`` (command: string, expected: string) =
  match run (pbotUsername expected) command with
  | Success(res, _, _) ->
    Assert.Equal(expected, res)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
        
[<Fact>]
let ``many1Usernames parses one+ usernames``() =
  let command = "@bot @bot2 @bot3 123"
  match run UsernameCommands.many1Usernames command with
  | Success(res, _, _) ->
    Assert.Equal<string seq>(res, ["@bot"; "@bot2"; "@bot3"])
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)

[<Fact>]
let ``pdistinctTimeFractions parses ban duration`` () =
  let command = "2 months 1 day 1 min"
  match run UsernameCommands.pdistinctTimeFractions command with
  | Success(Timed(res), _, _) ->
    let time = DateTime.UtcNow.AddMinutes(6.).AddMonths(2).AddDays(1.)
    Assert.Equal(time.ToString("yyyyMMddTHH:mm"), res.ToString("yyyyMMddTHH:mm"))
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()
        
[<Fact>]
let ``pforeverBan parses forever ban as eof`` () =
  let command = " \r\n\n\n\r    "
  match run UsernameCommands.pforeverBan command with
  | Success(Forever, _, _) ->
    Assert.Success()
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()
        
[<Fact>]
let ``pforeverBan parses forever ban as forever text`` () =
  let command = " forever   "
  match run UsernameCommands.pforeverBan command with
  | Success(Forever, _, _) ->
    Assert.Success()
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()
        
[<Theory>]
[<InlineData("@bot @first @second ban 1 day 12 months")>]
[<InlineData("    @bot @first @second ban 1 day 12 months")>]
[<InlineData(" \n\r \n@bot @first @second ban 1 day 12 months")>]
let ``parseCommand returns correct command for ban`` (command) =
  match run (UsernameCommands.parseCommand "@bot") command with
  | Success((Usernames(usernames), UsernameBan(Timed(duration))), _, _) ->
    let time = DateTime.UtcNow.AddMonths(12).AddDays(1.)
    Assert.Equal(time.ToString("yyyyMMddTHH:mm"), duration.ToString("yyyyMMddTHH:mm"))
    Assert.Equal<string seq>(["@first"; "@second"], usernames)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()

[<Theory>]
[<InlineData("@bot @first @second ban ")>]
[<InlineData("@bot @first@second ban ")>]
[<InlineData("@bot @first @second ban")>]
[<InlineData("@bot @first @second ban\r\n\n")>]
let ``parseCommand returns correct command for forever ban`` (command: string) =
  match run (UsernameCommands.parseCommand "@bot") command with
  | Success((Usernames(usernames), UsernameBan(Forever)), _, _) ->
    Assert.Equal<string seq>(["@first"; "@second"], usernames)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()
 
[<Fact>]
let ``parseCommand returns correct command for unban`` () =
  let command = "@bot @first @second unban"
  match run (UsernameCommands.parseCommand "@bot") command with
  | Success((Usernames(usernames), UsernameUnban), _, _) ->
    Assert.Equal<string seq>(["@first"; "@second"], usernames)
  | Failure(error, _, _ ) ->
    Assert.FailWithMessage(error)
  | _ ->
    Assert.Fail()
        
[<Theory>]
[<InlineData("@bot")>]
[<InlineData("@bot 123123")>]
[<InlineData("\r\n@bot ")>]
[<InlineData("\r\n@bot @text @text")>]
[<InlineData("\r\n@bot @text @text notban")>]
[<InlineData("@text @text ban 1 min 2 days")>]
[<InlineData("@bot1 @text @text ban 1 min 2 days")>]
let ``parseCommand fails`` (command: string) =
  match run (UsernameCommands.parseCommand "@bot") command with
  | Failure(error, _, _ ) ->
    Assert.Success()
  | _ ->
    Assert.Fail()