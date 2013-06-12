/// <reference path="vendor/angular/angular.js" />
/// <reference path="vendor/lodash/lodash.js" />
/*global angular:false */
'use strict';

var clusterManagerApp = angular.module('ClusterManagerApp', ['ui.bootstrap', 'ui.bootstrap.tpls']);

clusterManagerApp.config(function ($routeProvider, $locationProvider) {
    $routeProvider.when('/servers/*id', {
        templateUrl: '/views/serverExplorer.html',
        controller: 'ServerExplorerCtrl'
    });
    $routeProvider.when('/servers', {
        templateUrl: '/views/servers.html',
        controller: 'ServersCtrl'
    });   
    $routeProvider.when('/replication', {
        templateUrl: '/views/replication.html',
        controller: 'ReplicationCtrl'
    });

    $routeProvider.when('/api', {
        templateUrl: '/views/api.html',
        controller: 'apiCtrl'
    });
    
    $routeProvider.when('/api/servers/save-api', {
        templateUrl: '/views/servers/save-api.html',
        controller: 'apiCtrl'
    });
    $routeProvider.when('/createNewServer', {
        templateUrl: '/views/createNewServer.html',
        controller: 'CreateNewServerCtrl'
    });
    $routeProvider.when('/editServer', {
        templateUrl: '/views/editServer.html',
        controller: 'editServerCtrl'
    });
  
    

    

    
    $routeProvider.when('/', {
        templateUrl: '/views/main.html',
        controller: 'MainCtrl'
    });
    $routeProvider.otherwise({ redirectTo: '/' });
    
    $locationProvider.html5Mode(true);
});