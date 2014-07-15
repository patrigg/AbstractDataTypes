AbstractDataTypes
=================

Library and tool to define and test abstract data types. This library is for educational purposes to learn about abstract data types. It is written in C#. A simple tool called AdtPlayground is also included. It allows you to create abstract data types and also test them by evaluating various expressions and see the results after applying the axioms.


Installation
============

Currently there is no binary release available so you need to download the source and open the Visual Studio 2013 solution. The 2012 version of Visual Studio should also work fine but is not officially supported.


Getting Started
===============
To run the tool, simply open the solution and run the AdtPlayground project. The main window of the application consists of four regions depicted below:
===================X
|     |     |     |
|     |     |  3  |
|  1  |  2  |_____|
|     |     |     |
|     |     |  4  |
|_____|_____|_____|
|_____|_____|_____|

Region 1:
List of available abstract datatypes. In the current release four types are implemented by default: bool, Number, stack, queue. New types can be added here by clicking the New button on the bottom. Remove temporarily removes a type from the list. Reload updates all types with the definition previously saved.
