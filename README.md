# RSS Throttle
## Overview
RSS Throttle allows you to control the flow of an RSS feed so it’s available only at certain times of the day or at certain times of the week. You can also limit the number of items in the feed or filter by category.

### Use-Case
RSS Throttle was originally created as a means of reducing my digital dependancy on things like Twitter and news apps.

I found myself checking for new content on my phone all the time in seemingly addictive way, and also felt that following news stories too closely about political machinations (e.g. Brexit), crises (e.g. COVID) or societal injustice only ever resulted in me being in an unhappy or angry mood.

Too much content; far too frequent; and far too emotive!

My solution was to go back to my preferred pre-Twitter technology - RSS - with a decent reader app (Reeder) and then begin to control the flow of my content. 

For example:

* Give me the top 5 news stories from the Guardian RSS feed at 06:00 and 20:00 each day (avoids me browsing through the Guardian app endlessly);
* Give me all the news about products I like - e.g. Raspberry Pi, Termius, SetApp - but hold them all back until it’s the weekend when I have more free time;
* Give me 10 items of sports news between Monday to Friday at 06:00 and 20:00, but then don’t give my any weekend stories because I can’t always watch the games live and don’t want to know the score.

I can now limit the amount of stories I read and can control when they drop into my reader app. I also know there’s no point in me continually refreshing to try and get another dopamine hit of fresh content .

I use this package as part of an [Azure Function](https://github.com/cpwood/RSS-Throttle/tree/releases/1.0/src/AzureFunction).

## `When` values: Day and Hour Notation
These values are an important basic unit of how RSS Throttle works. They use a combination of day numbers (1-7) and hour numbers (00-23) either side of a “T”.

### Brief Examples

* Monday at 3pm: `1T15`
* Monday at 3pm and Saturday at 10am: `1T15,6T10`
* Monday, Tuesday, Wednesday, Thursday and Friday at 6pm: `1:5T18`
* Monday and Thursday at 4pm: `14T16`
* Every hour on Wednesday and Thursday: `34T*`
* Every day at 9am: `*T09`
* From midnight on Monday until 3pm on Friday, then from 3pm until 6pm on Saturday and Sunday: `1T00-5T15,6T15-6T18,7T15-7T18`

### Conventions

`When` values have the following notation:

```
<day(s)>T<hour(s)>
```

* Days are numeric values between `1` and `7` with Monday being `1` and `7` being Sunday.
* Hours are two-digit numeric values between `00` meaning midnight and `23` meaning 23:00 / 11pm.

```
1T15			Monday at 3pm
5T09			Friday at 9am
```

* Multiple values can appear either side of the `T`:

```
135T10		Monday, Wednesday and Friday at 10am
1T091218		Monday at 9am, 12pm and 6pm
12T1218		Monday and Tuesday at 12pm and 6pm
```

*  `*` can be used for Days or Hours and means “every day” or “each hour”.

```
*T15			Every day at 3pm
1T*			Monday at each hour
*T*			Every day at each hour
```

* Ranges of discrete values can be used for both Days and Hours using the `:` symbol. 

```
1:3T06		Monday, Tuesday and Wednesday at 6am
1T15:18		Monday at 3pm, 4pm, 5pm and 6pm
```

* Multiple `When` values can be presented by separating the values with commas.

```
1T15,3T18		Monday at 3pm and Wednesday at 6pm
```

* Windows of time can be expressed using the `-` symbol between two `When` values. NB: a **single value** must exist either side of `T` in this scenario.

```
6T00-1T00		From midnight on Saturday to midnight on Monday
```

* Day and Hour values are interpreted according to the UTC timezone, unless an alternative timezone is presented using a `Timezone` parameter.
* Minutes are _not_ supported.

## Parameters
These parameters are passed to an `RssThrottle.FeedService` instance’s `ProcessAsync` method using an `RssThrottle.Parameters` object. 

The `RssThrottle.Parameters` class has a `.Parse(string querystring)` method, allowing you to parse these values easily from a URL. When used in a URL, parameters use camelCase.

### Required Parameters
#### Url
This is the URL of the input RSS or Atom feed.

#### When
See guidance above.

#### Mode
Either `Delay`, `Include` or `Exclude`.

In `Delay` mode, any new input feed items will be held until the next available day and/or time. For example, the following combination of values will result in input feed items being held back until 06:00 and 18:00 each day. 

```
*T0618
```

When 06:00 occurs, anything published up to that time will appear in the output feed. If a an item is subsequently published to the input feed at 06:01, it won’t be available in the output feed until 18:00.

In  `Include` mode, only input feed items published _within_ one-or-more windows of time are included in the output feed. For example, to include input feed items published between midnight on Monday to 6pm on Friday:

```
1T00-5T18
```

Or to include input feed items published on Monday, Tuesday and Wednesday and between 3pm to 6pm:

```
1T15-1T18,2T15-2T18,3T15-3T18
```

In `Exclude` mode, only input feed items published _outside_ one-or-more windows of time are included in the output feed. The same time window notations are used as for `Include` mode, above.

### Optional Parameters
#### Timezone
The time zone to use when interpreting `When` values. 

Any [IANA Timezone name](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) is allowed - e.g. `Europe/London`. 

If not specified, the `UTC` timezone will be used.

#### Categories
Filters the input feed items by category. If this parameter isn’t specified, all input feed categories will be included in the output feed.

This is a comma-separated list of text values, e.g. `Planes,Trains,Automobiles` . 

A category may be prefixed with with `!` to indicate it _shouldn’t_ include a category, for example to exclude boats, `!Boats`

Categories for _inclusion_ are treated as “or” conditions, for example, `Planes,Trains,Automobiles` is interpreted as “planes or trains or automobiles”.

Categories for _exclusion_ are treated as “and” conditions, for example `!Boats,!Bicycles` is interpreted as “not boats and not bicycles”.

Included and excluded categories can be mixed. Included categories will be processed _before_ excluded categories. 

For example, `!Boats,!Bicycles,Transport,Travel` would be interpreted as “include anything from the Transport and Travel categories in an initial short list, but then remove anything that is also categorised under Boats and Bicycles from that short list”.

#### EnforceChronology
This will sort input feed items by the published date in descending order if set to `true`.

#### Limit
This is a positive integer value and will mean that the input items added to the output feed will be limited to the first _n_ items appearing in the short list of input feed items. 

For the avoidance of doubt, the input feed items _aren’t_ sorted prior to the limit taking effect unless `EnforceChronology` is set to `true`. The original ordering in the input feed is used when this value isn’t set.

If not specified, _all_ the available short list items will be returned.

### Read-Only Parameters

#### Hash

This provides a hash of the parameter values. This can be used to generate an identifier for a set of parameters which can then be used in an `ICache` implementation.

## Examples
These examples are from my own personal use of RSS Throttle within an Azure Function.

### Give me the top 5 news stories each day from the Guardian 
I only want a sense of what’s going on in the world and don’t want to see every available news story.

The Guardian RSS feed orders its feed according to importance rather than time - i.e. the top story is first in the input feed - so limiting the input items to 5 means I’m seeing the most important stuff.

Content is delayed until 06:00 and 20:00 (UK time) each day.

```
/feed?url=https://theguardian.com/uk/rss&mode=Delay&when=*T0620&timezone=Europe/London&limit=5
```

### Delay all Raspberry Pi news until the weekend
I’ve got more time for interests at the weekend and don’t want to be ignoring a growing “backlog” during the week.

Technically this is delaying input feed items until 09:00, 12:00, 18:00 and 21:00 (UK time) on Saturday and Sunday.

```
/feed?url=https://www.raspberrypi.org/blog/feed/&mode=Delay&when=67T09121821&timezone=Europe/London
```

### Give me the top 10 Super League stories each day
Super League is the top tier within the game of rugby league in the UK. I’m not interested in the lower tiers of the game, so I’ll only include input feed items from the “Super League” category.

Content is delayed until 06:00 and 20:00 (UK time) each day.

```
/feed?url=https://www.loverugbyleague.com/feed/&mode=Delay&when=1:5T0620&timezone=Europe/London&limit=10&categories=Super%20League
```

### Give me Australian Rules Football news at any point between Monday and Thursday, but exclude all content from Friday to Sunday
I’m a big Aussie Rules fan, but I live in the UK. Most games are played between Friday evening and Sunday evening (Australian time), which is early in the morning here in the UK.

Because of this, I watch most games delayed and don’t want to see a match  score accidentally. 

The easiest way of achieving this is by excluding all weekend news published between 3am on Friday and midnight on Monday (UK time).

```
/feed?url=https://www.foxsports.com.au/content-feeds/afl/&mode=Exclude&when=5T03-1T00&timezone=Europe/London
```

## Caching Delayed Output Feeds

The `FeedService` constructor has an optional `ICache` parameter that allows you to provide the included [`BlobStorageCache`](https://github.com/cpwood/RSS-Throttle/blob/main/src/RssThrottle/BlobStorageCache.cs) implementation or your own custom `ICache` implementation. This is used in the `Delay` mode of operation only.

This can be used to improve performance - i.e. so you don't have to generate the output feed from scratch each time - and also to bring a little extra reliability to the handling of input feeds. 

Since RSS Throttle is very reliant on publication `datetime` values in the input feeds, and since not all content platforms publish *consistent*, *unchanging* publication date values, using a cache may mean you don't get *slightly* different content each time you generate the output feed and will also mean you don't get content "leaking through" during periods you want to be quiet.

If you'd like to write your own `ICache` provider, take a look at the [Azure Blob Storage provider](https://github.com/cpwood/RSS-Throttle/blob/main/src/RssThrottle/BlobStorageCache.cs) for inspiration.

## Output feed titles, descriptions, copyright, etc

These will be copied over from the input feed.

## Missing input feed items
If the content provider’s input feed only contains 10 items, but they publish 20 items per day and you delay content to a single point in the week, you’ll miss out on most input feed items during that week.

RSS Throttle isn’t a miracle-worker and doesn’t cache input feed items. When it’s gone from the input feed, it’s gone forever.

## Including/Excluding and Delaying on the same input feed
It’s not possible to undertake two different `Mode` values against the same feed - I.e. you **can’t** do this:  `mode=Exclude,Delay`.

A workaround is to have a first `Exclude` feed and use its output as the input for a `Delay` feed.

## Development Credits
For reading input RSS and Atom feeds: [arminreiter/FeedReader](https://github.com/arminreiter/FeedReader)

Timezone-aware date and time calculations: [Noda Time](https://nodatime.org/)