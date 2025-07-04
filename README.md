<!--
SPDX-FileCopyrightText: 2017 PJB3005 <pieterjan.briers@gmail.com>
SPDX-FileCopyrightText: 2018 Pieter-Jan Briers <pieterjan.briers@gmail.com>
SPDX-FileCopyrightText: 2019 Ivan <silvertorch5@gmail.com>
SPDX-FileCopyrightText: 2019 Silver <silvertorch5@gmail.com>
SPDX-FileCopyrightText: 2020 Injazz <43905364+Injazz@users.noreply.github.com>
SPDX-FileCopyrightText: 2020 RedlineTriad <39059512+RedlineTriad@users.noreply.github.com>
SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <zddm@outlook.es>
SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
SPDX-FileCopyrightText: 2021 Swept <sweptwastaken@protonmail.com>
SPDX-FileCopyrightText: 2021 mirrorcult <lunarautomaton6@gmail.com>
SPDX-FileCopyrightText: 2022 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
SPDX-FileCopyrightText: 2022 ike709 <ike709@users.noreply.github.com>
SPDX-FileCopyrightText: 2023 iglov <iglov@avalon.land>
SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
SPDX-FileCopyrightText: 2024 Kira Bridgeton <161087999+Verbalase@users.noreply.github.com>
SPDX-FileCopyrightText: 2024 Rares Popa <2606875+rarepops@users.noreply.github.com>
SPDX-FileCopyrightText: 2024 router <messagebus@vk.com>
SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>

SPDX-License-Identifier: AGPL-3.0-or-later
-->

<p align="center"> <img alt="Space Station 14" width="880" height="300" src="https://github.com/Goob-Station/Goob-Station/blob/master/Resources/Textures/Logo/logo.png" /></p>

Tider's Shore is a Space Station 14 server that prides itself on uhh... *something*.. We haven't figured out what it exactly is yet.

We are a fork of [Goob Station](https://github.com/Goob-Station/Goob-Station), which itself is a fork of the [main Space Station 14 repository](https://github.com/space-wizards/space-station-14).
All SS14 forks, such as ours, utilise the [Robust Toolbox engine](https://github.com/space-wizards/RobustToolbox) as a framework — however, it is not directly a part of our repository, but a separate module.

If you want to host or create content for base SS14, go to the [Space Station 14 repository](https://github.com/space-wizards/space-station-14) as all base code and resources for SS14 forks.

## Documentation/Wiki

The Goob Station [docs site](https://docs.goobstation.com/) has documentation on GS14's (and by extension, also TS14's) content, engine, game design, and more, including code standards.

## Contributing

We are willing to accept contributions, however you are recommended to *not contribute to us*, unless you are confident in your ability to contribute, or are genuinely willing to learn!
Instead, we ask that you contribute to the [core SS14 codebase](https://github.com/space-wizards/space-station-14), as you will be likely to find better assistance there.

Although we are a fork of goob, you will be asked to fix your contributions if they are not sanely organised!
We suggest you do this by putting new code in separate directories, for example:
- Although all core gas prototypes are stored in `Resources/Prototypes/Atmospherics/gases.yml`, TS14-exclusive ones would go to `Resources/Prototypes/_TidersShore/Atmospherics/gases.yml`.
- Code for a new component being added to TS14, that would usually (in the core SS14 codebase) be added to `Content.Server/Atmos/Components`, would instead need go to `Content.Server/_TS14/Atmos/Components`.
- Edits that must be done in core files should have accompanying comments denoting them as non-core changes, like this `Explosive` component as an example:
<!-- zerowidth space to invalidate diff -->
```diff
​- type: entity
  name: pen
  id: Pen
  components:
  - type: Item
- - type: Explosive
+ - type: Explosive # TS14 Change
```

## Building

1. Clone this repo.
2a. Run `RUN_THIS.py` to init submodules and download the engine.
2b. Alternatively, run `git submodule update --init --recursive` in the root of your cloned repository.
3. Compile the solution.

See: [Goob codebase instructions for setting up the repository](https://docs.goobstation.com/en/general-development/setup.html)

## License

All code in this codebase is released under the AGPL-3.0-or-later license. Each file includes REUSE Specification headers or separate .license files that specify a dual license option. This dual licensing is provided to simplify the process for projects that are not using AGPL, allowing them to adopt the relevant portions of the code under an alternative license. You can review the complete texts of these licenses in the LICENSES/ directory.

Most media assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.
